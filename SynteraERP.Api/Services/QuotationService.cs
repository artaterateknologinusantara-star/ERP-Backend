using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Quotation;
using SynteraERP.Api.Helpers;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class QuotationService : IQuotationService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public QuotationService(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

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
            .Include(x => x.Tabs).ThenInclude(t => t.Groups).ThenInclude(g => g.Subcontractor)
            .Include(x => x.Tabs).ThenInclude(t => t.Groups).ThenInclude(g => g.WorkItems).ThenInclude(w => w.WorkDetails).ThenInclude(d => d.Attachments)
            .Include(x => x.Termins)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (q is null) return null;

        string? approvedByName = null;
        if (q.ApprovedBy.HasValue)
        {
            var approver = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == q.ApprovedBy.Value);
            approvedByName = approver?.Name;
        }

        return ToDto(q, approvedByName);
    }

    public async Task<QuotationDto> CreateAsync(SaveQuotationRequest request)
    {
        ValidateTermins(request.Termins);
        var no = await NextNumberAsync();
        var quotation = MapFromRequest(request, no);
        _db.Quotations.Add(quotation);
        await _db.SaveChangesAsync();
        return (await GetByIdAsync(quotation.Id))!;
    }

    public async Task<QuotationDto?> UpdateAsync(Guid id, SaveQuotationRequest request)
    {
        ValidateTermins(request.Termins);

        // Tracked (not AsNoTracking) — Tabs/Groups/Items are upserted in place below, so the
        // change tracker needs to see what already exists to diff against.
        var quotation = await _db.Quotations
            .Include(x => x.Tabs).ThenInclude(t => t.Groups).ThenInclude(g => g.Items)
            .Include(x => x.Tabs).ThenInclude(t => t.Groups).ThenInclude(g => g.WorkItems).ThenInclude(w => w.WorkDetails)
            .Include(x => x.Termins)
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
        quotation.IsCivilMeMode = request.IsCivilMeMode;
        quotation.TotalAreaSqm = request.TotalAreaSqm;
        quotation.UpdatedAt = DateTimeOffset.UtcNow;

        await using var tx = await _db.Database.BeginTransactionAsync();

        // Upsert-by-Id for Tabs/Groups — NOT a blind delete+rebuild. Group.Id must survive a
        // normal save because QuotationWorkItem (RAB/BQ Item Pekerjaan/Detail Kerja, plus their
        // uploaded images) is FK'd to it; destroying and recreating the Group on every "Submit
        // Penawaran" silently orphaned/cascaded-deleted that data (confirmed by an end-to-end
        // regression check before this fix). Items still get replaced wholesale per group below —
        // nothing external hangs off QuotationItem.Id today, so that stays simple.
        UpsertTabs(quotation, request.Tabs);

        // Full replace, not upsert-by-Id like Tabs/Groups — nothing hangs off QuotationTermin.Id
        // (no attachment, no downstream FK), so there's no data to orphan by recreating rows on
        // every save. Same reasoning as QuotationItem's per-group replace above.
        _db.QuotationTermins.RemoveRange(quotation.Termins);
        quotation.Termins = request.Termins.Select(t => new QuotationTermin
        {
            QuotationId = quotation.Id,
            SortOrder = t.SortOrder,
            Description = t.Description,
            Percentage = t.Percentage,
        }).ToList();

        RecalcTotals(quotation);

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return (await GetByIdAsync(id))!;
    }

    // Termins are optional (old quotations keep using free-text PaymentTerms) but when the caller
    // does send a structured list, it must add up to a full 100% — a partial list would silently
    // gate Invoice creation at less than the SO's real value later. 0.01 tolerance absorbs
    // decimal(5,2) rounding on the percentage split (e.g. 33.33 x 3 = 99.99, not 100).
    private static void ValidateTermins(List<SaveQuotationTerminRequest> termins)
    {
        if (termins.Count == 0) return;

        var total = termins.Sum(t => t.Percentage);
        if (Math.Abs(total - 100m) > 0.01m)
            throw new InvalidOperationException(
                $"Total persentase termin harus 100%, saat ini {total}%.");
    }

    private void UpsertTabs(Models.Quotation quotation, List<SaveQuotationTabRequest> incomingTabs)
    {
        var incomingTabIds = incomingTabs.Where(t => t.Id.HasValue).Select(t => t.Id!.Value).ToHashSet();
        foreach (var tab in quotation.Tabs.Where(t => !incomingTabIds.Contains(t.Id)).ToList())
            _db.QuotationTabs.Remove(tab);

        foreach (var incoming in incomingTabs)
        {
            var tab = incoming.Id.HasValue ? quotation.Tabs.FirstOrDefault(t => t.Id == incoming.Id.Value) : null;
            if (tab is null)
            {
                // Explicit DbSet.Add — a plain `quotation.Tabs.Add(tab)` relies on graph fixup,
                // which (since Id is a client-generated Guid, already non-default) makes EF assume
                // this row already exists and mark it Modified instead of Added, causing an UPDATE
                // against a row that was never inserted → DbUpdateConcurrencyException on save.
                tab = new QuotationTab { QuotationId = quotation.Id, Label = incoming.Label, SortOrder = incoming.SortOrder };
                _db.QuotationTabs.Add(tab);
                quotation.Tabs.Add(tab);
            }
            else
            {
                tab.Label = incoming.Label;
                tab.SortOrder = incoming.SortOrder;
            }

            UpsertGroups(tab, incoming.Groups);
        }
    }

    private void UpsertGroups(QuotationTab tab, List<SaveQuotationGroupRequest> incomingGroups)
    {
        var incomingGroupIds = incomingGroups.Where(g => g.Id.HasValue).Select(g => g.Id!.Value).ToHashSet();
        foreach (var group in tab.Groups.Where(g => !incomingGroupIds.Contains(g.Id)).ToList())
            _db.QuotationGroups.Remove(group);

        foreach (var incoming in incomingGroups)
        {
            var group = incoming.Id.HasValue ? tab.Groups.FirstOrDefault(g => g.Id == incoming.Id.Value) : null;
            if (group is null)
            {
                // Same reasoning as the Tab Add above — explicit DbSet.Add to force Added state.
                group = new QuotationGroup { TabId = tab.Id };
                _db.QuotationGroups.Add(group);
                tab.Groups.Add(group);
            }

            group.Name = incoming.Name;
            group.SortOrder = incoming.SortOrder;
            group.RecapVolume = incoming.RecapVolume;
            group.RecapUnit = incoming.RecapUnit;
            group.SubcontractorId = incoming.SubcontractorId;
            group.FinalSubconCost = incoming.FinalSubconCost;
            group.FinalSellingPrice = incoming.FinalSellingPrice;

            // Items have no children of their own (no attachment hangs off Item.Id) — a full
            // per-group replace is still the simplest correct approach for them.
            group.Items.Clear();
            foreach (var item in incoming.Items)
            {
                // Same explicit-Add reasoning as Tab/Group above.
                var newItem = new QuotationItem
                {
                    GroupId = group.Id,
                    ItemNo = item.ItemNo,
                    Equipment = item.Equipment,
                    Description = item.Description,
                    Manufacturer = item.Manufacturer,
                    Qty = item.Qty,
                    Unit = item.Unit,
                    ServicePrice = item.ServicePrice,
                    MaterialPrice = item.MaterialPrice,
                    Length = item.Length,
                    Width = item.Width,
                    Height = item.Height,
                    SortOrder = item.SortOrder,
                };
                // NOT also `group.Items.Add(newItem)` — `GroupId` is already set above, and
                // `group` is a tracked entity (loaded via Include at the top of UpdateAsync), so
                // EF Core's relationship fixup already adds `newItem` to `group.Items` as a side
                // effect of the line below. Adding it explicitly too used to double every entry
                // in the in-memory collection (DB stayed correct — one row per item — but any
                // in-memory sum over `group.Items` right after this method, e.g. RecalcTotals,
                // silently double-counted every item for both Civil ME and standard mode on any
                // UpdateAsync call with existing items).
                _db.QuotationItems.Add(newItem);
            }
        }
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
            .Include(x => x.Tabs).ThenInclude(t => t.Groups).ThenInclude(g => g.WorkItems).ThenInclude(w => w.WorkDetails)
            .Include(x => x.Termins)
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
            IsCivilMeMode = source.IsCivilMeMode,
            TotalAreaSqm = source.TotalAreaSqm,
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
                RecapVolume = g.RecapVolume,
                RecapUnit = g.RecapUnit,
                SubcontractorId = g.SubcontractorId,
                FinalSubconCost = g.FinalSubconCost,
                FinalSellingPrice = g.FinalSellingPrice,
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
                    Length = i.Length,
                    Width = i.Width,
                    Height = i.Height,
                    SortOrder = i.SortOrder,
                }).ToList(),
                // Attachments (gambar) sengaja TIDAK ikut disalin — file per-dokumen asli, wajar
                // tidak ikut ke duplikat/revisi baru. Data teks/angka RAB/BQ tetap disalin penuh.
                WorkItems = g.WorkItems.Select(w => new QuotationWorkItem
                {
                    GroupId = Guid.Empty,
                    Name = w.Name,
                    SortOrder = w.SortOrder,
                    WorkDetails = w.WorkDetails.Select(d => new QuotationWorkDetail
                    {
                        WorkItemId = Guid.Empty,
                        Name = d.Name,
                        Spesifikasi = d.Spesifikasi,
                        Volume = d.Volume,
                        Unit = d.Unit,
                        UnitPrice = d.UnitPrice,
                        SortOrder = d.SortOrder,
                    }).ToList(),
                }).ToList(),
            }).ToList(),
        }).ToList();

        copy.Termins = source.Termins.Select(t => new QuotationTermin
        {
            SortOrder = t.SortOrder,
            Description = t.Description,
            Percentage = t.Percentage,
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
            .Include(x => x.Tabs).ThenInclude(t => t.Groups).ThenInclude(g => g.WorkItems).ThenInclude(w => w.WorkDetails)
            .Include(x => x.Termins)
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
            IsCivilMeMode = source.IsCivilMeMode,
            TotalAreaSqm = source.TotalAreaSqm,
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
                RecapVolume = g.RecapVolume,
                RecapUnit = g.RecapUnit,
                SubcontractorId = g.SubcontractorId,
                FinalSubconCost = g.FinalSubconCost,
                FinalSellingPrice = g.FinalSellingPrice,
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
                    Length = i.Length,
                    Width = i.Width,
                    Height = i.Height,
                    SortOrder = i.SortOrder,
                }).ToList(),
                // Attachments (gambar) sengaja TIDAK ikut disalin — file per-dokumen asli, wajar
                // tidak ikut ke duplikat/revisi baru. Data teks/angka RAB/BQ tetap disalin penuh.
                WorkItems = g.WorkItems.Select(w => new QuotationWorkItem
                {
                    Name = w.Name,
                    SortOrder = w.SortOrder,
                    WorkDetails = w.WorkDetails.Select(d => new QuotationWorkDetail
                    {
                        Name = d.Name,
                        Spesifikasi = d.Spesifikasi,
                        Volume = d.Volume,
                        Unit = d.Unit,
                        UnitPrice = d.UnitPrice,
                        SortOrder = d.SortOrder,
                    }).ToList(),
                }).ToList(),
            }).ToList(),
        }).ToList();

        revision.Termins = source.Termins.Select(t => new QuotationTermin
        {
            SortOrder = t.SortOrder,
            Description = t.Description,
            Percentage = t.Percentage,
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

    // ── Item Pekerjaan / Detail Kerja (RAB/BQ) — auto-save per baris, ID stabil ──
    // sepanjang sesi edit (bukan bagian SaveQuotationRequest) supaya lampiran gambar tidak
    // ikut hilang tiap kali quotation induk di-Update (lihat BuildTabs/UpdateAsync di atas).

    public async Task<QuotationWorkItemDto> CreateWorkItemAsync(Guid groupId, SaveWorkItemRequest request)
    {
        var group = await _db.QuotationGroups.FirstOrDefaultAsync(g => g.Id == groupId)
            ?? throw new KeyNotFoundException("Group tidak ditemukan.");

        var workItem = new QuotationWorkItem
        {
            GroupId = groupId,
            Name = request.Name,
            SortOrder = request.SortOrder,
        };
        _db.QuotationWorkItems.Add(workItem);
        await _db.SaveChangesAsync();
        return ToWorkItemDto(workItem);
    }

    public async Task<bool> UpdateWorkItemAsync(Guid id, SaveWorkItemRequest request)
    {
        var workItem = await _db.QuotationWorkItems.FirstOrDefaultAsync(w => w.Id == id);
        if (workItem is null) return false;

        workItem.Name = request.Name;
        workItem.SortOrder = request.SortOrder;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteWorkItemAsync(Guid id)
    {
        var workItem = await _db.QuotationWorkItems
            .Include(w => w.WorkDetails).ThenInclude(d => d.Attachments)
            .FirstOrDefaultAsync(w => w.Id == id);
        if (workItem is null) return false;

        foreach (var attachment in workItem.WorkDetails.SelectMany(d => d.Attachments))
            DeleteAttachmentFile(attachment.FilePath);

        _db.QuotationWorkItems.Remove(workItem);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<QuotationWorkDetailDto> CreateWorkDetailAsync(Guid workItemId, SaveWorkDetailRequest request)
    {
        var workItem = await _db.QuotationWorkItems.FirstOrDefaultAsync(w => w.Id == workItemId)
            ?? throw new KeyNotFoundException("Item Pekerjaan tidak ditemukan.");

        var detail = new QuotationWorkDetail
        {
            WorkItemId = workItemId,
            Name = request.Name,
            Spesifikasi = request.Spesifikasi,
            Volume = request.Volume,
            Unit = request.Unit,
            UnitPrice = request.UnitPrice,
            SortOrder = request.SortOrder,
        };
        _db.QuotationWorkDetails.Add(detail);
        await _db.SaveChangesAsync();
        return ToWorkDetailDto(detail);
    }

    public async Task<bool> UpdateWorkDetailAsync(Guid id, SaveWorkDetailRequest request)
    {
        var detail = await _db.QuotationWorkDetails.FirstOrDefaultAsync(d => d.Id == id);
        if (detail is null) return false;

        detail.Name = request.Name;
        detail.Spesifikasi = request.Spesifikasi;
        detail.Volume = request.Volume;
        detail.Unit = request.Unit;
        detail.UnitPrice = request.UnitPrice;
        detail.SortOrder = request.SortOrder;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteWorkDetailAsync(Guid id)
    {
        var detail = await _db.QuotationWorkDetails
            .Include(d => d.Attachments)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (detail is null) return false;

        foreach (var attachment in detail.Attachments)
            DeleteAttachmentFile(attachment.FilePath);

        _db.QuotationWorkDetails.Remove(detail);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<QuotationWorkDetailAttachmentDto> UploadWorkDetailAttachmentAsync(Guid workDetailId, IFormFile file)
    {
        var detail = await _db.QuotationWorkDetails.FirstOrDefaultAsync(d => d.Id == workDetailId)
            ?? throw new KeyNotFoundException("Detail Kerja tidak ditemukan.");

        if (file is null || file.Length == 0)
            throw new ArgumentException("File gambar tidak boleh kosong.");

        const long maxBytes = 1 * 1024 * 1024; // 1MB
        if (file.Length > maxBytes)
            throw new ArgumentException("Ukuran gambar maksimal 1MB.");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var bytes = ms.ToArray();

        // Magic-byte check — reject anything that isn't actually PNG/JPEG, regardless of extension/content-type.
        var isPng = bytes.Length >= 8
            && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47
            && bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A;
        var isJpeg = bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
        if (!isPng && !isJpeg)
            throw new ArgumentException("File harus berupa gambar JPG atau PNG yang valid.");

        var contentType = isPng ? "image/png" : "image/jpeg";
        var ext = isPng ? ".png" : ".jpg";

        var uploadsDir = Path.Combine(_env.ContentRootPath, "uploads", "quotation-work-detail");
        Directory.CreateDirectory(uploadsDir);

        var storedName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(uploadsDir, storedName);
        await File.WriteAllBytesAsync(fullPath, bytes);

        var nextSortOrder = (await _db.QuotationWorkDetailAttachments
            .Where(a => a.WorkDetailId == workDetailId)
            .Select(a => (int?)a.SortOrder)
            .MaxAsync()) ?? -1;

        var attachment = new QuotationWorkDetailAttachment
        {
            WorkDetailId = workDetailId,
            FilePath = Path.Combine("quotation-work-detail", storedName),
            FileName = file.FileName,
            ContentType = contentType,
            SortOrder = nextSortOrder + 1,
        };
        _db.QuotationWorkDetailAttachments.Add(attachment);
        await _db.SaveChangesAsync();

        return new QuotationWorkDetailAttachmentDto
        {
            Id = attachment.Id,
            FileName = attachment.FileName,
            SortOrder = attachment.SortOrder,
        };
    }

    public async Task<bool> DeleteWorkDetailAttachmentAsync(Guid attachmentId)
    {
        var attachment = await _db.QuotationWorkDetailAttachments.FirstOrDefaultAsync(a => a.Id == attachmentId);
        if (attachment is null) return false;

        DeleteAttachmentFile(attachment.FilePath);
        _db.QuotationWorkDetailAttachments.Remove(attachment);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<(byte[] data, string contentType, string fileName)?> GetWorkDetailAttachmentAsync(Guid attachmentId)
    {
        var attachment = await _db.QuotationWorkDetailAttachments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == attachmentId);
        if (attachment is null) return null;

        var fullPath = Path.Combine(_env.ContentRootPath, "uploads", attachment.FilePath);
        if (!File.Exists(fullPath)) return null;

        var data = await File.ReadAllBytesAsync(fullPath);
        return (data, attachment.ContentType, attachment.FileName);
    }

    private void DeleteAttachmentFile(string relativePath)
    {
        var fullPath = Path.Combine(_env.ContentRootPath, "uploads", relativePath);
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }

    private static QuotationWorkItemDto ToWorkItemDto(QuotationWorkItem w) => new()
    {
        Id = w.Id,
        Name = w.Name,
        SortOrder = w.SortOrder,
        WorkDetails = w.WorkDetails.OrderBy(d => d.SortOrder).Select(ToWorkDetailDto).ToList(),
    };

    private static QuotationWorkDetailDto ToWorkDetailDto(QuotationWorkDetail d) => new()
    {
        Id = d.Id,
        Name = d.Name,
        Spesifikasi = d.Spesifikasi,
        Volume = d.Volume,
        Unit = d.Unit,
        UnitPrice = d.UnitPrice,
        TotalHarga = d.TotalHarga,
        SortOrder = d.SortOrder,
        Attachments = d.Attachments.OrderBy(a => a.SortOrder)
            .Select(a => new QuotationWorkDetailAttachmentDto { Id = a.Id, FileName = a.FileName, SortOrder = a.SortOrder })
            .ToList(),
    };

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
            IsCivilMeMode = req.IsCivilMeMode,
            TotalAreaSqm = req.TotalAreaSqm,
            Status = QuotationStatus.Draft,
        };
        q.Tabs = BuildTabs(req.Tabs, q.Id);
        q.Termins = req.Termins.Select(t => new QuotationTermin
        {
            QuotationId = q.Id,
            SortOrder = t.SortOrder,
            Description = t.Description,
            Percentage = t.Percentage,
        }).ToList();
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
                RecapVolume = g.RecapVolume,
                RecapUnit = g.RecapUnit,
                SubcontractorId = g.SubcontractorId,
                FinalSubconCost = g.FinalSubconCost,
                FinalSellingPrice = g.FinalSellingPrice,
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
                    Length = i.Length,
                    Width = i.Width,
                    Height = i.Height,
                    SortOrder = i.SortOrder,
                }).ToList(),
            }).ToList(),
        }).ToList();

    private static void RecalcTotals(Models.Quotation q)
    {
        var allGroups = q.Tabs.SelectMany(t => t.Groups).ToList();
        if (q.IsCivilMeMode)
        {
            // Civil & ME total is 3 sources added together: FinalSellingPrice (Subkontraktor SOW,
            // harga jual ke customer — FinalSubconCost is cost-basis only, used for margin, never
            // summed here), QuotationItem (equipment/material lines, same as standard mode — Qty *
            // MaterialPrice goes to TotalMaterial, Qty * ServicePrice to TotalService), and
            // QuotationWorkDetail/BOQ (TotalHarga is a single blended price — no Jasa/Material
            // split source, so it's added to TotalService alongside FinalSellingPrice).
            var civilMeItems = allGroups.SelectMany(g => g.Items).ToList();
            var allWorkDetails = allGroups.SelectMany(g => g.WorkItems).SelectMany(w => w.WorkDetails).ToList();
            q.TotalMaterial = MoneyMath.Round(civilMeItems.Sum(i => i.Qty * i.MaterialPrice));
            q.TotalService = MoneyMath.Round(
                allGroups.Sum(g => g.FinalSellingPrice ?? 0)
                + civilMeItems.Sum(i => i.Qty * i.ServicePrice)
                + allWorkDetails.Sum(d => d.TotalHarga));
        }
        else
        {
            var allItems = allGroups.SelectMany(g => g.Items).ToList();
            q.TotalMaterial = MoneyMath.Round(allItems.Sum(i => i.Qty * i.MaterialPrice));
            q.TotalService = MoneyMath.Round(allItems.Sum(i => i.Qty * i.ServicePrice));
        }
        var subtotal = q.TotalMaterial + q.TotalService;
        var discountAmount = MoneyMath.Round(subtotal * q.Discount / 100);
        q.TotalBeforeTax = subtotal - discountAmount;
        q.TaxAmount = MoneyMath.Round(q.TotalBeforeTax * q.TaxRate / 100);
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

    private static QuotationDto ToDto(Models.Quotation x, string? approvedByName = null) => new()
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
        IsCivilMeMode = x.IsCivilMeMode,
        TotalAreaSqm = x.TotalAreaSqm,
        ParentId = x.ParentId,
        ApprovedAt = x.ApprovedAt,
        ApprovedByName = approvedByName,
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
                RecapVolume = g.RecapVolume,
                RecapUnit = g.RecapUnit,
                SubcontractorId = g.SubcontractorId,
                SubcontractorName = g.Subcontractor?.Name,
                FinalSubconCost = g.FinalSubconCost,
                FinalSellingPrice = g.FinalSellingPrice,
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
                    Length = i.Length,
                    Width = i.Width,
                    Height = i.Height,
                    SortOrder = i.SortOrder,
                }).ToList(),
                WorkItems = g.WorkItems.OrderBy(w => w.SortOrder).Select(ToWorkItemDto).ToList(),
            }).ToList(),
        }).ToList(),
        Termins = x.Termins.OrderBy(t => t.SortOrder).Select(t => new QuotationTerminDto
        {
            Id = t.Id,
            SortOrder = t.SortOrder,
            Description = t.Description,
            Percentage = t.Percentage,
        }).ToList(),
    };
}
