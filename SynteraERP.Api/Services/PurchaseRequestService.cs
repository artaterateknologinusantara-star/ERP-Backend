using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Purchasing;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class PurchaseRequestService : IPurchaseRequestService
{
    private readonly AppDbContext _db;

    public PurchaseRequestService(AppDbContext db) => _db = db;

    public async Task<PaginatedResponse<PurchaseRequestListDto>> ListAsync(PaginationParams p)
    {
        var q = _db.PurchaseRequests
            .AsNoTracking()
            .Include(x => x.RequestedByUser)
            .Include(x => x.SalesOrder)
            .Include(x => x.Items)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(p.Search))
        {
            var s = p.Search.ToLower();
            q = q.Where(x => x.No.ToLower().Contains(s) || x.RequestedByUser.Name.ToLower().Contains(s));
        }

        q = p.SortBy switch
        {
            "no"     => p.IsDescending ? q.OrderByDescending(x => x.No)     : q.OrderBy(x => x.No),
            "date"   => p.IsDescending ? q.OrderByDescending(x => x.Date)   : q.OrderBy(x => x.Date),
            "status" => p.IsDescending ? q.OrderByDescending(x => x.Status) : q.OrderBy(x => x.Status),
            _        => q.OrderByDescending(x => x.CreatedAt),
        };

        var total = await q.CountAsync();
        var data  = await q.Skip(p.Skip).Take(p.PerPage)
            .Select(x => ToListDto(x))
            .ToListAsync();

        return PaginatedResponse<PurchaseRequestListDto>.Create(data, total, p.Page, p.PerPage);
    }

    public async Task<PurchaseRequestDto?> GetByIdAsync(Guid id)
    {
        var pr = await _db.PurchaseRequests
            .AsNoTracking()
            .Include(x => x.RequestedByUser)
            .Include(x => x.SalesOrder)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (pr is null) return null;

        // Load approver name if approved
        string? approvedByName = null;
        if (pr.ApprovedByUserId.HasValue)
        {
            var approver = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == pr.ApprovedByUserId.Value);
            approvedByName = approver?.Name;
        }

        return ToDto(pr, approvedByName);
    }

    public async Task<PurchaseRequestDto> CreateAsync(CreatePurchaseRequestRequest request)
    {
        var no = await NextNumberAsync();
        var pr = new Models.PurchaseRequest
        {
            No            = no,
            RequestedBy   = request.RequestedBy,
            SalesOrderId  = request.SalesOrderId,
            Date          = request.Date,
            Notes         = request.Notes,
            Status        = PurchaseRequestStatus.Draft,
            Items         = request.Items.Select(i => new PurchaseRequestItem
            {
                ItemName = i.ItemName,
                Qty      = i.Qty,
                Unit     = i.Unit,
                EstPrice = i.EstPrice,
                Notes    = i.Notes,
            }).ToList(),
        };

        pr.Total = pr.Items.Sum(i => i.Qty * i.EstPrice);

        _db.PurchaseRequests.Add(pr);
        await _db.SaveChangesAsync();
        return (await GetByIdAsync(pr.Id))!;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status, Guid? userId = null)
    {
        var pr = await _db.PurchaseRequests.FindAsync(id);
        if (pr is null) return false;

        if (!Enum.TryParse<PurchaseRequestStatus>(status, true, out var parsed)) return false;

        // Validate transition
        var validTransitions = new Dictionary<PurchaseRequestStatus, List<PurchaseRequestStatus>>
        {
            [PurchaseRequestStatus.Draft]     = [PurchaseRequestStatus.Submitted],
            [PurchaseRequestStatus.Submitted] = [PurchaseRequestStatus.Approved, PurchaseRequestStatus.Rejected],
            [PurchaseRequestStatus.Rejected]  = [PurchaseRequestStatus.Draft],
            [PurchaseRequestStatus.Approved]  = [PurchaseRequestStatus.Ordered],
        };

        if (validTransitions.TryGetValue(pr.Status, out var allowed) && !allowed.Contains(parsed))
            return false;

        pr.Status    = parsed;
        pr.UpdatedAt = DateTimeOffset.UtcNow;

        if (parsed == PurchaseRequestStatus.Approved)
        {
            pr.ApprovedAt       = DateTimeOffset.UtcNow;
            pr.ApprovedByUserId = userId;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var pr = await _db.PurchaseRequests.FindAsync(id);
        if (pr is null) return false;

        pr.IsDeleted = true;
        pr.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Generate PR from Sales Order ──────────────────────────────────────────

    public async Task<PurchaseRequestDto?> GenerateFromSoAsync(Guid soId, Guid requestedBy)
    {
        var so = await _db.SalesOrders
            .Include(x => x.Items.OrderBy(i => i.SortOrder))
            .Include(x => x.Quotation)
                .ThenInclude(q => q!.Tabs)
                    .ThenInclude(t => t.Groups)
                        .ThenInclude(g => g.Items)
            .FirstOrDefaultAsync(x => x.Id == soId && !x.IsDeleted);

        if (so is null) return null;

        // Check for existing non-rejected PR on this SO
        var existingPr = await _db.PurchaseRequests
            .Where(x => x.SalesOrderId == soId
                && x.Status != PurchaseRequestStatus.Rejected
                && !x.IsDeleted)
            .FirstOrDefaultAsync();

        var notes = $"Generated dari SO {so.No}";
        if (existingPr is not null)
            notes += $" (PR sebelumnya: {existingPr.No})";

        // Build item list
        var items = new List<PurchaseRequestItem>();

        if (so.QuotationId.HasValue && so.Quotation is not null)
        {
            // Source: Quotation material items (MaterialPrice > 0)
            var quotationItems = so.Quotation.Tabs
                .SelectMany(t => t.Groups)
                .SelectMany(g => g.Items)
                .Where(i => i.MaterialPrice > 0)
                .ToList();

            foreach (var qItem in quotationItems)
            {
                var itemMaster = await _db.ItemMasters
                    .FirstOrDefaultAsync(x =>
                        x.Name.ToLower().Contains(qItem.Equipment.ToLower()) && x.IsActive);

                decimal estPrice;
                bool priceNeedsVerification;

                if (itemMaster?.PurchasePrice != null)
                {
                    estPrice = itemMaster.PurchasePrice.Value;
                    priceNeedsVerification = false;
                }
                else
                {
                    estPrice = qItem.MaterialPrice;
                    priceNeedsVerification = true;
                }

                items.Add(new PurchaseRequestItem
                {
                    ItemName     = qItem.Equipment,
                    Qty          = qItem.Qty,
                    Unit         = qItem.Unit,
                    EstPrice     = estPrice,
                    ItemMasterId = itemMaster?.Id,
                    Notes        = priceNeedsVerification
                        ? $"{qItem.Description} [Harga belum diverifikasi Purchasing — estimasi dari Quotation]".Trim()
                        : qItem.Description,
                });
            }
        }
        else
        {
            // Source: SalesOrderItems (SO tanpa Quotation)
            foreach (var soItem in so.Items)
            {
                items.Add(new PurchaseRequestItem
                {
                    ItemName = soItem.Description,
                    Qty      = soItem.Qty,
                    Unit     = soItem.Uom,
                    EstPrice = soItem.UnitPrice,
                    Notes    = soItem.Notes,
                });
            }
        }

        var no = await NextNumberAsync();
        var pr = new Models.PurchaseRequest
        {
            No           = no,
            SalesOrderId = soId,
            RequestedBy  = requestedBy,
            Date         = DateOnly.FromDateTime(DateTime.UtcNow),
            Status       = PurchaseRequestStatus.Draft,
            Notes        = notes,
            Items        = items,
        };

        pr.Total = pr.Items.Sum(i => i.Qty * i.EstPrice);

        _db.PurchaseRequests.Add(pr);
        await _db.SaveChangesAsync();
        return (await GetByIdAsync(pr.Id))!;
    }

    // ── Stats ─────────────────────────────────────────────────────────────────

    public async Task<PurchaseRequestStatsDto> GetStatsAsync()
    {
        var q = _db.PurchaseRequests.Where(x => !x.IsDeleted);

        return new PurchaseRequestStatsDto
        {
            Total     = await q.CountAsync(),
            Draft     = await q.CountAsync(x => x.Status == PurchaseRequestStatus.Draft),
            Submitted = await q.CountAsync(x => x.Status == PurchaseRequestStatus.Submitted),
            Approved  = await q.CountAsync(x => x.Status == PurchaseRequestStatus.Approved),
            Rejected  = await q.CountAsync(x => x.Status == PurchaseRequestStatus.Rejected),
            TotalValue = await q.SumAsync(x => x.Total),
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<string> NextNumberAsync()
    {
        var config = await _db.NumberingConfigs
            .FirstOrDefaultAsync(n => n.DocType == "PURCHASE_REQUEST")
            ?? throw new InvalidOperationException("NumberingConfig for PURCHASE_REQUEST not found");

        var no = config.GenerateNext();
        await _db.SaveChangesAsync();
        return no;
    }

    private static PurchaseRequestListDto ToListDto(Models.PurchaseRequest x) => new()
    {
        Id              = x.Id,
        No              = x.No,
        Date            = x.Date,
        RequestedByName = x.RequestedByUser?.Name ?? string.Empty,
        SalesOrderNo    = x.SalesOrder?.No,
        Status          = x.Status.ToString(),
        Total           = x.Total,
        Notes           = x.Notes,
        ItemCount       = x.Items.Count,
    };

    private static PurchaseRequestDto ToDto(Models.PurchaseRequest x, string? approvedByName = null)
    {
        var items = x.Items.Select(i => new PurchaseRequestItemDto
        {
            Id         = i.Id,
            ItemName   = i.ItemName,
            Qty        = i.Qty,
            Unit       = i.Unit,
            EstPrice   = i.EstPrice,
            Notes      = i.Notes,
            OrderedQty = i.OrderedQty,
        }).ToList();

        return new PurchaseRequestDto
        {
            Id              = x.Id,
            No              = x.No,
            Date            = x.Date,
            RequestedByName = x.RequestedByUser?.Name ?? string.Empty,
            SalesOrderNo    = x.SalesOrder?.No,
            Status          = x.Status.ToString(),
            Total           = x.Total,
            Notes           = x.Notes,
            RequestedBy     = x.RequestedBy,
            SalesOrderId    = x.SalesOrderId,
            ApprovedByName  = approvedByName,
            CreatedAt       = x.CreatedAt,
            ItemsNeedingPriceVerification = items.Count(i =>
                i.Notes != null && i.Notes.Contains("belum diverifikasi Purchasing")),
            Items = items,
        };
    }
}
