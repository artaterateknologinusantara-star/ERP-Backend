using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Quotation;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class QuotationService : IQuotationService
{
    private readonly AppDbContext _db;

    public QuotationService(AppDbContext db) => _db = db;

    public async Task<PaginatedResponse<QuotationListDto>> ListAsync(PaginationParams p)
    {
        var q = _db.Quotations
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Sales)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(p.Search))
        {
            var s = p.Search.ToLower();
            q = q.Where(x => x.No.ToLower().Contains(s) || x.ProjectName.ToLower().Contains(s)
                || x.Customer.Name.ToLower().Contains(s));
        }

        q = p.SortBy switch
        {
            "no" => p.IsDescending ? q.OrderByDescending(x => x.No) : q.OrderBy(x => x.No),
            "date" => p.IsDescending ? q.OrderByDescending(x => x.Date) : q.OrderBy(x => x.Date),
            "grandTotal" => p.IsDescending ? q.OrderByDescending(x => x.GrandTotal) : q.OrderBy(x => x.GrandTotal),
            "status" => p.IsDescending ? q.OrderByDescending(x => x.Status) : q.OrderBy(x => x.Status),
            _ => p.IsDescending ? q.OrderByDescending(x => x.CreatedAt) : q.OrderByDescending(x => x.CreatedAt),
        };

        var total = await q.CountAsync();
        var items = await q.Skip(p.Skip).Take(p.PerPage).ToListAsync();

        var ids = items.Select(x => x.Id).ToList();
        var cpoQuotationIds = (await _db.CustomerPOs
            .Where(c => ids.Contains(c.QuotationId))
            .Select(c => c.QuotationId)
            .ToListAsync()).ToHashSet();

        var data = items.Select(x => ToListDto(x, cpoQuotationIds.Contains(x.Id))).ToList();
        return PaginatedResponse<QuotationListDto>.Create(data, total, p.Page, p.PerPage);
    }

    public async Task<QuotationDto?> GetByIdAsync(Guid id)
    {
        var q = await _db.Quotations
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Sales)
            .Include(x => x.Tabs).ThenInclude(t => t.Groups).ThenInclude(g => g.Items)
            .FirstOrDefaultAsync(x => x.Id == id);

        return q is null ? null : ToDto(q);
    }

    public async Task<QuotationDto> CreateAsync(SaveQuotationRequest request)
    {
        var no = await NextNumberAsync();
        var quotation = MapFromRequest(request, no);
        _db.Quotations.Add(quotation);
        await _db.SaveChangesAsync();
        return (await GetByIdAsync(quotation.Id))!;
    }

    public async Task<QuotationDto?> UpdateAsync(Guid id, SaveQuotationRequest request)
    {
        var quotation = await _db.Quotations
            .FirstOrDefaultAsync(x => x.Id == id);

        if (quotation is null) return null;

        quotation.CustomerId = request.CustomerId;
        quotation.SalesId = request.SalesId;
        quotation.ProjectName = request.ProjectName;
        quotation.Date = request.Date;
        quotation.ValidUntil = request.ValidUntil ?? default;
        quotation.Notes = request.Notes;
        quotation.PaymentTerms = request.PaymentTerms;
        quotation.TermsAndConditions = request.TermsAndConditions;
        quotation.AdditionalNotes = request.AdditionalNotes;
        quotation.Discount = request.Discount;
        quotation.TaxRate = request.TaxRate;
        quotation.UpdatedAt = DateTimeOffset.UtcNow;

        // Delete old tabs at DB level (cascades to groups/items) — avoids EF change-tracker conflicts
        await _db.QuotationTabs.Where(t => t.QuotationId == id).ExecuteDeleteAsync();

        // Insert new tabs
        var newTabs = BuildTabs(request.Tabs, quotation.Id);
        _db.QuotationTabs.AddRange(newTabs);
        quotation.Tabs = newTabs;
        RecalcTotals(quotation);

        await _db.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status)
    {
        var quotation = await _db.Quotations.FindAsync(id);
        if (quotation is null) return false;

        if (!Enum.TryParse<QuotationStatus>(status, true, out var parsed)) return false;

        quotation.Status = parsed;
        quotation.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<QuotationDto> DuplicateAsync(Guid id)
    {
        var source = await _db.Quotations
            .Include(x => x.Tabs).ThenInclude(t => t.Groups).ThenInclude(g => g.Items)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Quotation {id} not found");

        var no = await NextNumberAsync();
        var copy = new Models.Quotation
        {
            No = no,
            CustomerId = source.CustomerId,
            SalesId = source.SalesId,
            ProjectName = source.ProjectName + " (Copy)",
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            ValidUntil = source.ValidUntil,
            Notes = source.Notes,
            PaymentTerms = source.PaymentTerms,
            TermsAndConditions = source.TermsAndConditions,
            AdditionalNotes = source.AdditionalNotes,
            Discount = source.Discount,
            TaxRate = source.TaxRate,
            Status = QuotationStatus.Draft,
            ParentId = source.Id,
        };

        copy.Tabs = source.Tabs.Select(t => new QuotationTab
        {
            QuotationId = copy.Id,
            Label = t.Label,
            SortOrder = t.SortOrder,
            Groups = t.Groups.Select(g => new QuotationGroup
            {
                TabId = Guid.Empty, // set by EF via nav
                Name = g.Name,
                SortOrder = g.SortOrder,
                Items = g.Items.Select(i => new QuotationItem
                {
                    GroupId = Guid.Empty,
                    ItemNo = i.ItemNo,
                    Equipment = i.Equipment,
                    Description = i.Description,
                    Manufacturer = i.Manufacturer,
                    Qty = i.Qty,
                    Unit = i.Unit,
                    ServicePrice = i.ServicePrice,
                    MaterialPrice = i.MaterialPrice,
                    SortOrder = i.SortOrder,
                }).ToList(),
            }).ToList(),
        }).ToList();

        RecalcTotals(copy);
        _db.Quotations.Add(copy);
        await _db.SaveChangesAsync();
        return (await GetByIdAsync(copy.Id))!;
    }

    public async Task<SendQuotationResultDto?> SendAsync(Guid id, Guid sentByUserId)
    {
        var quotation = await _db.Quotations
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (quotation is null) return null;

        quotation.Status = QuotationStatus.Terkirim;
        quotation.SentAt = DateTimeOffset.UtcNow;
        quotation.SentBy = sentByUserId;
        quotation.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        return new SendQuotationResultDto
        {
            QuotationNo = quotation.No,
            ProjectName = quotation.ProjectName,
            CustomerName = quotation.Customer?.Name ?? string.Empty,
            CustomerEmail = quotation.Customer?.Email,
            SentAt = quotation.SentAt!.Value,
        };
    }

    public async Task<QuotationDto?> CreateRevisionAsync(Guid id)
    {
        var source = await _db.Quotations
            .Include(x => x.Tabs).ThenInclude(t => t.Groups).ThenInclude(g => g.Items)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (source is null) return null;
        if (source.Status is not (QuotationStatus.Terkirim or QuotationStatus.Disetujui or QuotationStatus.Selesai)) return null;

        source.IsLatestRevision = false;
        source.UpdatedAt = DateTimeOffset.UtcNow;

        // Selesai → Superseded (CPO already exists); Terkirim → Direvisi
        source.Status = source.Status == QuotationStatus.Selesai
            ? QuotationStatus.Superseded
            : QuotationStatus.Direvisi;

        // New revision keeps SAME quotation number, increments revision counter
        var revision = new Models.Quotation
        {
            No = source.No,
            CustomerId = source.CustomerId,
            SalesId = source.SalesId,
            ProjectName = source.ProjectName,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            ValidUntil = source.ValidUntil,
            Notes = source.Notes,
            PaymentTerms = source.PaymentTerms,
            TermsAndConditions = source.TermsAndConditions,
            AdditionalNotes = source.AdditionalNotes,
            Discount = source.Discount,
            TaxRate = source.TaxRate,
            Status = QuotationStatus.Draft,
            Revision = source.Revision + 1,
            ParentId = source.ParentId ?? source.Id,
            IsLatestRevision = true,
        };

        revision.Tabs = source.Tabs.Select(t => new QuotationTab
        {
            QuotationId = revision.Id,
            Label = t.Label,
            SortOrder = t.SortOrder,
            Groups = t.Groups.Select(g => new QuotationGroup
            {
                Name = g.Name,
                SortOrder = g.SortOrder,
                Items = g.Items.Select(i => new QuotationItem
                {
                    ItemNo = i.ItemNo,
                    Equipment = i.Equipment,
                    Description = i.Description,
                    Manufacturer = i.Manufacturer,
                    Qty = i.Qty,
                    Unit = i.Unit,
                    ServicePrice = i.ServicePrice,
                    MaterialPrice = i.MaterialPrice,
                    SortOrder = i.SortOrder,
                }).ToList(),
            }).ToList(),
        }).ToList();

        RecalcTotals(revision);
        _db.Quotations.Add(revision);

        // Link superseded source back to the new revision for audit trail
        if (source.Status == QuotationStatus.Superseded)
            source.SupersededByQuotationId = revision.Id;

        await _db.SaveChangesAsync();
        return (await GetByIdAsync(revision.Id))!;
    }

    public async Task<bool> ApproveAsync(Guid id, Guid approvedByUserId)
    {
        var quotation = await _db.Quotations.FindAsync(id);
        if (quotation is null || quotation.Status != QuotationStatus.Terkirim) return false;

        quotation.Status = QuotationStatus.Disetujui;
        quotation.ApprovedAt = DateTimeOffset.UtcNow;
        quotation.ApprovedBy = approvedByUserId;
        quotation.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectAsync(Guid id)
    {
        var quotation = await _db.Quotations.FindAsync(id);
        if (quotation is null || quotation.Status != QuotationStatus.Terkirim) return false;

        quotation.Status = QuotationStatus.Ditolak;
        quotation.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var quotation = await _db.Quotations.FindAsync(id);
        if (quotation is null) return false;

        quotation.IsDeleted = true;
        quotation.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task<string> NextNumberAsync()
    {
        var config = await _db.NumberingConfigs
            .FirstOrDefaultAsync(n => n.DocType == "QUOTATION")
            ?? throw new InvalidOperationException("NumberingConfig for QUOTATION not found");

        // Sync LastNumber with the actual highest number in DB.
        // Prevents unique-constraint violations when a migration reset the counter
        // while existing quotation rows with higher numbers still exist.
        var year = DateTime.UtcNow.ToString("yy");
        var yearPrefix = $"{config.Prefix}-{year}.";

        var existingNos = await _db.Quotations
            .IgnoreQueryFilters()
            .Where(q => q.No.StartsWith(yearPrefix))
            .Select(q => q.No)
            .ToListAsync();

        if (existingNos.Count > 0)
        {
            var actualMax = existingNos
                .Select(no =>
                {
                    var suffix = no.Length > yearPrefix.Length ? no[yearPrefix.Length..] : "0";
                    return int.TryParse(suffix, out var n) ? n : 0;
                })
                .Max();

            if (actualMax >= config.LastNumber)
                config.LastNumber = actualMax;
        }

        var docNo = config.GenerateNext();
        await _db.SaveChangesAsync();
        return docNo;
    }

    private static Models.Quotation MapFromRequest(SaveQuotationRequest req, string no)
    {
        var q = new Models.Quotation
        {
            No = no,
            CustomerId = req.CustomerId,
            SalesId = req.SalesId,
            ProjectName = req.ProjectName,
            Date = req.Date,
            ValidUntil = req.ValidUntil ?? default,
            Notes = req.Notes,
            PaymentTerms = req.PaymentTerms,
            TermsAndConditions = req.TermsAndConditions,
            AdditionalNotes = req.AdditionalNotes,
            Discount = req.Discount,
            TaxRate = req.TaxRate,
            Status = QuotationStatus.Draft,
        };
        q.Tabs = BuildTabs(req.Tabs, q.Id);
        RecalcTotals(q);
        return q;
    }

    private static List<QuotationTab> BuildTabs(List<SaveQuotationTabRequest> tabs, Guid quotationId) =>
        tabs.Select(t => new QuotationTab
        {
            QuotationId = quotationId,
            Label = t.Label,
            SortOrder = t.SortOrder,
            Groups = t.Groups.Select(g => new QuotationGroup
            {
                Name = g.Name,
                SortOrder = g.SortOrder,
                Items = g.Items.Select(i => new QuotationItem
                {
                    ItemNo = i.ItemNo,
                    Equipment = i.Equipment,
                    Description = i.Description,
                    Manufacturer = i.Manufacturer,
                    Qty = i.Qty,
                    Unit = i.Unit,
                    ServicePrice = i.ServicePrice,
                    MaterialPrice = i.MaterialPrice,
                    SortOrder = i.SortOrder,
                }).ToList(),
            }).ToList(),
        }).ToList();

    private static void RecalcTotals(Models.Quotation q)
    {
        var allItems = q.Tabs.SelectMany(t => t.Groups).SelectMany(g => g.Items).ToList();
        q.TotalMaterial = allItems.Sum(i => i.Qty * i.MaterialPrice);
        q.TotalService = allItems.Sum(i => i.Qty * i.ServicePrice);
        var subtotal = q.TotalMaterial + q.TotalService;
        var discountAmount = subtotal * q.Discount / 100;
        q.TotalBeforeTax = subtotal - discountAmount;
        q.TaxAmount = q.TotalBeforeTax * q.TaxRate / 100;
        q.GrandTotal = q.TotalBeforeTax + q.TaxAmount;
    }

    private static QuotationListDto ToListDto(Models.Quotation x, bool hasCustomerPO = false) => new()
    {
        Id = x.Id,
        No = x.No,
        Date = x.Date,
        ProjectName = x.ProjectName,
        CustomerName = x.Customer?.Name ?? string.Empty,
        CustomerEmail = x.Customer?.Email,
        SalesName = x.Sales?.Name ?? string.Empty,
        Status = x.Status.ToString(),
        GrandTotal = x.GrandTotal,
        ValidUntil = x.ValidUntil,
        Revision = x.Revision,
        IsLatestRevision = x.IsLatestRevision,
        SentAt = x.SentAt,
        HasCustomerPO = hasCustomerPO,
    };

    private static QuotationDto ToDto(Models.Quotation x) => new()
    {
        Id = x.Id,
        No = x.No,
        Date = x.Date,
        ProjectName = x.ProjectName,
        CustomerName = x.Customer?.Name ?? string.Empty,
        CustomerEmail = x.Customer?.Email,
        SalesName = x.Sales?.Name ?? string.Empty,
        Status = x.Status.ToString(),
        GrandTotal = x.GrandTotal,
        ValidUntil = x.ValidUntil,
        CustomerId = x.CustomerId,
        SalesId = x.SalesId,
        Revision = x.Revision,
        IsLatestRevision = x.IsLatestRevision,
        SentAt = x.SentAt,
        Notes = x.Notes,
        PaymentTerms = x.PaymentTerms,
        TermsAndConditions = x.TermsAndConditions,
        AdditionalNotes = x.AdditionalNotes,
        Discount = x.Discount,
        TaxRate = x.TaxRate,
        TotalMaterial = x.TotalMaterial,
        TotalService = x.TotalService,
        TotalBeforeTax = x.TotalBeforeTax,
        TaxAmount = x.TaxAmount,
        ParentId = x.ParentId,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt,
        Tabs = x.Tabs.OrderBy(t => t.SortOrder).Select(t => new QuotationTabDto
        {
            Id = t.Id,
            Label = t.Label,
            SortOrder = t.SortOrder,
            Groups = t.Groups.OrderBy(g => g.SortOrder).Select(g => new QuotationGroupDto
            {
                Id = g.Id,
                Name = g.Name,
                SortOrder = g.SortOrder,
                Items = g.Items.OrderBy(i => i.SortOrder).Select(i => new QuotationItemDto
                {
                    Id = i.Id,
                    ItemNo = i.ItemNo,
                    Equipment = i.Equipment,
                    Description = i.Description,
                    Manufacturer = i.Manufacturer,
                    Qty = i.Qty,
                    Unit = i.Unit,
                    ServicePrice = i.ServicePrice,
                    MaterialPrice = i.MaterialPrice,
                    SortOrder = i.SortOrder,
                }).ToList(),
            }).ToList(),
        }).ToList(),
    };
}
