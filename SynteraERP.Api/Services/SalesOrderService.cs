using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.SalesOrder;
using SynteraERP.Api.Helpers;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class SalesOrderService : ISalesOrderService
{
    private readonly AppDbContext _db;
    private readonly ITaxRateService _taxRateService;

    public SalesOrderService(AppDbContext db, ITaxRateService taxRateService)
    {
        _db = db;
        _taxRateService = taxRateService;
    }

    public async Task<PaginatedResponse<SalesOrderListResponse>> GetListAsync(int page, int perPage, string? search, string? status)
    {
        var q = _db.SalesOrders
            .AsNoTracking()
            .Include(x => x.Customer)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            q = q.Where(x => x.No.ToLower().Contains(s)
                || x.Customer.Name.ToLower().Contains(s)
                || x.ProjectName.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SalesOrderStatus>(status, true, out var statusEnum))
            q = q.Where(x => x.Status == statusEnum);

        q = q.OrderByDescending(x => x.CreatedAt);

        var total = await q.CountAsync();
        var skip = (page - 1) * perPage;
        var data = await q.Skip(skip).Take(perPage)
            .Select(x => new SalesOrderListResponse
            {
                Id = x.Id,
                No = x.No,
                CustomerName = x.Customer.Name,
                ProjectName = x.ProjectName,
                Date = x.Date,
                ExpectedDate = x.ExpectedDate,
                Status = x.Status.ToString(),
                GrandTotal = x.Total,
                RefQuotation = x.RefQuotation,
            })
            .ToListAsync();

        await AttachPhasesAsync(data);

        return PaginatedResponse<SalesOrderListResponse>.Create(data, total, page, perPage);
    }

    // Computes the same workflow phase as the "Progress SO" stepper on the detail page, in
    // batched queries scoped to just this page's SOs (not the fetch-everything-then-filter-client
    // -side approach the detail page uses) so the list stays cheap regardless of PR/PO volume.
    // Decision logic itself lives in ComputeOpenPhase/ComputeStaticPhase, shared with GetByIdAsync's
    // single-SO path below, so the two never drift apart.
    private async Task AttachPhasesAsync(List<SalesOrderListResponse> data)
    {
        var soIds = data
            .Where(x => x.Status is "Open")
            .Select(x => x.Id)
            .ToList();

        if (soIds.Count == 0)
        {
            foreach (var row in data)
                row.Phase = ComputeStaticPhase(row.Status);
            return;
        }

        var allPRs = await _db.PurchaseRequests
            .AsNoTracking()
            .Where(pr => pr.SalesOrderId != null && soIds.Contains(pr.SalesOrderId.Value))
            .OrderByDescending(pr => pr.CreatedAt)
            .Select(pr => new { pr.Id, pr.SalesOrderId, pr.Status, ItemCount = pr.Items.Count })
            .ToListAsync();

        // Latest PR per SO decides the phase's PR-status/item-count inputs...
        var latestPRBySo = allPRs
            .GroupBy(pr => pr.SalesOrderId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        // ...but hasPOs/allCompleted must look at POs from EVERY PR linked to the SO, not just the
        // latest one — an SO can accumulate more than one PR over time, and a PO already Completed
        // against an earlier PR still counts. Map every PR back to its SO so POs can be grouped at
        // the SO level.
        var soIdByPrId = allPRs.ToDictionary(pr => pr.Id, pr => pr.SalesOrderId!.Value);
        var allPrIds = allPRs.Select(pr => pr.Id).ToList();

        var poStatsBySo = new Dictionary<Guid, (bool HasPOs, bool AllCompleted)>();
        if (allPrIds.Count > 0)
        {
            var pos = await _db.PurchaseOrders
                .AsNoTracking()
                .Where(po => po.PurchaseRequestId != null && allPrIds.Contains(po.PurchaseRequestId.Value))
                .Select(po => new { po.PurchaseRequestId, po.Status })
                .ToListAsync();

            poStatsBySo = pos
                .GroupBy(po => soIdByPrId[po.PurchaseRequestId!.Value])
                .ToDictionary(
                    g => g.Key,
                    g => (HasPOs: true, AllCompleted: g.All(po => po.Status == PurchaseOrderStatus.Completed)));
        }

        foreach (var row in data)
        {
            if (row.Status != "Open")
            {
                row.Phase = ComputeStaticPhase(row.Status);
                continue;
            }

            if (!latestPRBySo.TryGetValue(row.Id, out var pr))
            {
                row.Phase = "pr-needed";
                continue;
            }

            var (hasPOs, allCompleted) = poStatsBySo.TryGetValue(row.Id, out var stats) ? stats : (false, false);
            row.Phase = ComputeOpenPhase(pr.Status, pr.ItemCount, hasPOs, allCompleted);
        }
    }

    // Phase for a single SO's "Open" status — same decision inputs as AttachPhasesAsync above
    // (latest PR's status/item count, plus hasPOs/allCompleted across every PR linked to the SO),
    // just fetched directly for one SO instead of batched across a page of them.
    private async Task<string> ComputeOpenPhaseForSoAsync(Guid soId)
    {
        var prs = await _db.PurchaseRequests
            .AsNoTracking()
            .Where(pr => pr.SalesOrderId == soId)
            .OrderByDescending(pr => pr.CreatedAt)
            .Select(pr => new { pr.Id, pr.Status, ItemCount = pr.Items.Count })
            .ToListAsync();

        if (prs.Count == 0) return "pr-needed";

        var latestPr = prs[0];
        var prIds = prs.Select(pr => pr.Id).ToList();

        var poStatuses = await _db.PurchaseOrders
            .AsNoTracking()
            .Where(po => po.PurchaseRequestId != null && prIds.Contains(po.PurchaseRequestId.Value))
            .Select(po => po.Status)
            .ToListAsync();

        var hasPOs = poStatuses.Count > 0;
        var allCompleted = hasPOs && poStatuses.All(s => s == PurchaseOrderStatus.Completed);

        return ComputeOpenPhase(latestPr.Status, latestPr.ItemCount, hasPOs, allCompleted);
    }

    private static string ComputeOpenPhase(PurchaseRequestStatus prStatus, int prItemCount, bool hasPOs, bool allCompleted)
    {
        var prHasItems = prItemCount > 0;
        var prFullyDone = prStatus == PurchaseRequestStatus.Ordered && allCompleted;

        if (prHasItems && !prFullyDone)
            return prStatus == PurchaseRequestStatus.Ordered && hasPOs ? "gr-pending" : "pr-processing";

        return "do-ready";
    }

    private static string ComputeStaticPhase(string status) => status switch
    {
        "Cancelled" => "cancelled",
        "Completed" => "completed",
        "Delivered" => "invoice-ready",
        _ => "do-ready",
    };

    public async Task<SalesOrderDetailResponse?> GetByIdAsync(Guid id)
    {
        var so = await _db.SalesOrders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Sales)
            .Include(x => x.Items.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (so is null) return null;

        var taxRate = await _taxRateService.GetDefaultRateAsync();
        var statusStr = so.Status.ToString();
        var phase = statusStr == "Open" ? await ComputeOpenPhaseForSoAsync(so.Id) : ComputeStaticPhase(statusStr);
        return ToDetailResponse(so, taxRate, phase);
    }

    public async Task<SalesOrderDetailResponse> CreateAsync(CreateSalesOrderRequest request)
    {
        if (request.RetentionPercentage < 0 || request.RetentionPercentage > 100)
            throw new ArgumentException("RetentionPercentage harus di antara 0 dan 100.");

        string? terms = request.Terms;
        string? refQuotation = request.RefQuotation;

        if (request.QuotationId.HasValue)
        {
            var quotation = await _db.Quotations.FindAsync(request.QuotationId.Value)
                ?? throw new KeyNotFoundException("Quotation tidak ditemukan.");

            if (quotation.Status != QuotationStatus.Disetujui && quotation.Status != QuotationStatus.Selesai)
                throw new InvalidOperationException("Quotation belum disetujui atau Customer PO belum diterima.");

            var hasCpo = await _db.CustomerPOs.AnyAsync(c => c.QuotationId == request.QuotationId.Value);
            if (!hasCpo)
                throw new InvalidOperationException("Customer PO belum diterima.");

            if (string.IsNullOrWhiteSpace(refQuotation))
                refQuotation = $"{quotation.No} R{quotation.Revision}";

            if (string.IsNullOrWhiteSpace(terms))
                terms = quotation.PaymentTerms;
        }

        var items = request.Items.Select(dto => new SalesOrderItem
        {
            ItemMasterId = dto.ItemMasterId,
            Description = dto.Description,
            Sku = dto.Sku,
            Qty = dto.Qty,
            Uom = dto.Uom,
            UnitPrice = dto.UnitPrice,
            Discount = dto.Discount,
            Amount = Math.Round(dto.Qty * dto.UnitPrice * (1 - dto.Discount / 100), 2),
            Notes = dto.Notes,
            SortOrder = dto.SortOrder,
        }).ToList();

        var taxRate = await _taxRateService.GetDefaultRateAsync();
        var subTotal = Math.Round(items.Sum(x => x.Amount), 2);
        var taxAmount = Math.Round(subTotal * taxRate, 2);
        var grandTotal = subTotal + taxAmount;

        var no = await NextNumberAsync();

        var so = new SalesOrder
        {
            No = no,
            CustomerId = request.CustomerId,
            ProjectName = request.ProjectName,
            SalesId = request.SalesId,
            Notes = request.Notes,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            ExpectedDate = request.ExpectedDate,
            ShipTo = request.ShipTo,
            Terms = terms,
            RefQuotation = refQuotation,
            QuotationId = request.QuotationId,
            Status = SalesOrderStatus.Open,
            Total = grandTotal,
            RetentionPercentage = request.RetentionPercentage,
            Items = items,
        };

        await using var tx = await _db.Database.BeginTransactionAsync();

        _db.SalesOrders.Add(so);
        _db.Projects.Add(await BuildProjectForSoAsync(so, grandTotal));
        await _db.SaveChangesAsync();

        await tx.CommitAsync();

        return (await GetByIdAsync(so.Id))!;
    }

    public async Task UpdateStatusAsync(Guid id, string status)
    {
        var so = await _db.SalesOrders.FindAsync(id)
            ?? throw new KeyNotFoundException("Sales Order tidak ditemukan.");

        if (!Enum.TryParse<SalesOrderStatus>(status, true, out var parsed))
            throw new ArgumentException($"Status '{status}' tidak valid.");

        so.Status = parsed;
        so.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var so = await _db.SalesOrders.FindAsync(id)
            ?? throw new KeyNotFoundException("Sales Order tidak ditemukan.");

        so.IsDeleted = true;
        so.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<SalesOrderStatsResponse> GetStatsAsync()
    {
        var today = DateTime.UtcNow;
        var firstOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return new SalesOrderStatsResponse
        {
            Total = await _db.SalesOrders.CountAsync(x => !x.IsDeleted),
            Open = await _db.SalesOrders.CountAsync(x => !x.IsDeleted && x.Status == SalesOrderStatus.Open),
            Delivered = await _db.SalesOrders.CountAsync(x => !x.IsDeleted && x.Status == SalesOrderStatus.Delivered),
            CompletedThisMonth = await _db.SalesOrders.CountAsync(x => !x.IsDeleted
                && x.Status == SalesOrderStatus.Completed
                && x.UpdatedAt >= firstOfMonth),
            TotalValue = await _db.SalesOrders.Where(x => !x.IsDeleted).SumAsync(x => x.Total),
        };
    }

    public async Task<SalesOrderDetailResponse> CreateFromQuotationAsync(Guid quotationId, Guid userId)
    {
        var quotation = await _db.Quotations
            .Include(x => x.Customer)
            .Include(x => x.Tabs)
                .ThenInclude(t => t.Groups)
                    .ThenInclude(g => g.Items)
            .FirstOrDefaultAsync(x => x.Id == quotationId && !x.IsDeleted)
            ?? throw new Exception("Quotation tidak ditemukan");

        if (quotation.Status != QuotationStatus.Disetujui && quotation.Status != QuotationStatus.Selesai)
            throw new Exception("Quotation harus berstatus Disetujui atau Selesai");

        var existingSO = await _db.SalesOrders
            .FirstOrDefaultAsync(x => x.QuotationId == quotationId && !x.IsDeleted);
        if (existingSO != null)
            return (await GetByIdAsync(existingSO.Id))!;

        var allItems = quotation.Tabs
            .OrderBy(t => t.SortOrder)
            .SelectMany(t => t.Groups.OrderBy(g => g.SortOrder))
            .SelectMany(g => g.Items.OrderBy(i => i.SortOrder))
            .ToList();

        var soItems = allItems.Select((item, index) => new SalesOrderItem
        {
            Id = Guid.NewGuid(),
            Description = item.Equipment,
            Sku = item.ItemNo,
            Qty = item.Qty,
            Uom = item.Unit,
            UnitPrice = item.MaterialPrice + item.ServicePrice,
            Discount = 0,
            Amount = Math.Round(item.Qty * (item.MaterialPrice + item.ServicePrice), 2),
            QtyShipped = 0,
            Notes = item.Description,
            SortOrder = index,
        }).ToList();

        var taxRate = await _taxRateService.GetDefaultRateAsync();
        var subTotal = Math.Round(soItems.Sum(x => x.Amount), 2);
        var taxAmount = Math.Round(subTotal * taxRate, 2);
        var grandTotal = subTotal + taxAmount;

        var no = await NextNumberAsync();

        var so = new SalesOrder
        {
            No = no,
            QuotationId = quotationId,
            CustomerId = quotation.CustomerId,
            ProjectName = quotation.ProjectName,
            SalesId = quotation.SalesId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Terms = quotation.PaymentTerms,
            RefQuotation = $"{quotation.No} R{quotation.Revision}",
            Status = SalesOrderStatus.Open,
            Total = grandTotal,
            Notes = null,
            Items = soItems,
        };

        await using var tx = await _db.Database.BeginTransactionAsync();

        _db.SalesOrders.Add(so);

        if (quotation.Status == QuotationStatus.Disetujui)
            quotation.Status = QuotationStatus.Selesai;

        // SO, the quotation status flip, and Project are all added to the context before the one
        // SaveChangesAsync below — not a separate save-then-retry for the Project. A prior version
        // split them so a Project.Code collision could self-heal via
        // SequentialCodeHelper.RunWithRetryAsync without burning a fresh SO number on retry. In
        // practice that left orphan SOs with no Project behind whenever the retry budget was
        // exhausted (observed twice during testing). One transaction means ANY failure here —
        // QuotationId collision or otherwise — rolls back the SO too; the caller just retries the
        // whole request, and the existingSO check above confirms there's nothing left to clean up.
        _db.Projects.Add(await BuildProjectForSoAsync(so, grandTotal));

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsQuotationAlreadyHasSoViolation(ex))
        {
            // Two concurrent requests (e.g. a double-submit) can both pass the existingSO == null
            // check above before either commits — the unique index on SalesOrders.QuotationId is
            // what actually prevents the duplicate SO. The loser lands here instead of a raw 500;
            // roll back (the failed insert leaves the transaction unusable for further writes) and
            // hand back the winner's SO exactly like the existingSO check above would have.
            await tx.RollbackAsync();
            _db.ChangeTracker.Clear();
            var winner = await _db.SalesOrders.FirstOrDefaultAsync(x => x.QuotationId == quotationId && !x.IsDeleted);
            if (winner is null) throw;
            return (await GetByIdAsync(winner.Id))!;
        }

        await tx.CommitAsync();

        return (await GetByIdAsync(so.Id))!;
    }

    private static bool IsQuotationAlreadyHasSoViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sqlEx
        && (sqlEx.Number == 2601 || sqlEx.Number == 2627)
        && sqlEx.Message.Contains("IX_SalesOrders_QuotationId", StringComparison.OrdinalIgnoreCase);

    private async Task<Project> BuildProjectForSoAsync(SalesOrder so, decimal budget)
    {
        var code  = await SequentialCodeHelper.NextYearCodeAsync(_db.Projects, "PRJ", 3, DateTime.UtcNow.Year);
        var name  = !string.IsNullOrWhiteSpace(so.ProjectName) ? so.ProjectName : so.No;

        return new Project
        {
            Code        = code,
            Name        = name,
            CustomerId  = so.CustomerId,
            SalesOrderId = so.Id,
            Budget      = budget,
            Status      = ProjectStatus.Planning,
            StartDate   = DateOnly.FromDateTime(DateTime.UtcNow),
        };
    }

    private async Task<string> NextNumberAsync()
    {
        var config = await _db.NumberingConfigs
            .FirstOrDefaultAsync(n => n.DocType == "SALES_ORDER")
            ?? throw new InvalidOperationException("NumberingConfig for SALES_ORDER not found");

        var no = config.GenerateNext();
        await _db.SaveChangesAsync();
        return no;
    }

    private static SalesOrderDetailResponse ToDetailResponse(SalesOrder so, decimal taxRate, string phase)
    {
        var subTotal = so.Items.Any()
            ? Math.Round(so.Items.Sum(x => x.Amount), 2)
            : Math.Round(so.Total / (1 + taxRate), 2);

        var taxAmount = Math.Round(subTotal * taxRate, 2);
        var grandTotal = so.Items.Any() ? subTotal + taxAmount : so.Total;

        return new SalesOrderDetailResponse
        {
            Id = so.Id,
            No = so.No,
            CustomerId = so.CustomerId,
            CustomerName = so.Customer?.Name ?? string.Empty,
            CustomerAddress = so.Customer?.Address,
            CustomerNpwp = so.Customer?.Npwp,
            CustomerContactPerson = so.Customer?.ContactPerson,
            ProjectName = so.ProjectName,
            SalesId = so.SalesId,
            SalesName = so.Sales?.Name ?? string.Empty,
            Date = so.Date,
            ExpectedDate = so.ExpectedDate,
            ShipTo = so.ShipTo,
            Terms = so.Terms,
            RefQuotation = so.RefQuotation,
            Notes = so.Notes,
            Status = so.Status.ToString(),
            Phase = phase,
            SubTotal = subTotal,
            TaxAmount = taxAmount,
            GrandTotal = grandTotal,
            RetentionPercentage = so.RetentionPercentage,
            Items = so.Items.Select(i => new SalesOrderItemResponse
            {
                Id = i.Id,
                ItemMasterId = i.ItemMasterId,
                Description = i.Description,
                Sku = i.Sku,
                Qty = i.Qty,
                Uom = i.Uom,
                UnitPrice = i.UnitPrice,
                Discount = i.Discount,
                Amount = i.Amount,
                QtyShipped = i.QtyShipped,
                QtyNotShipped = i.Qty - i.QtyShipped,
                Notes = i.Notes,
                SortOrder = i.SortOrder,
            }).ToList(),
            CreatedAt = so.CreatedAt,
        };
    }
}
