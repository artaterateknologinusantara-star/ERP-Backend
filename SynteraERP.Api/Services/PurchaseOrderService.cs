using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Purchasing;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly AppDbContext _db;

    public PurchaseOrderService(AppDbContext db) => _db = db;

    public async Task<PaginatedResponse<PurchaseOrderListDto>> ListAsync(PaginationParams p)
    {
        var q = _db.PurchaseOrders
            .Include(x => x.Supplier)
            .Include(x => x.PurchaseRequest)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(p.Search))
        {
            var s = p.Search.ToLower();
            q = q.Where(x => x.No.ToLower().Contains(s) || x.Supplier.Name.ToLower().Contains(s));
        }

        q = p.SortBy switch
        {
            "no" => p.IsDescending ? q.OrderByDescending(x => x.No) : q.OrderBy(x => x.No),
            "date" => p.IsDescending ? q.OrderByDescending(x => x.Date) : q.OrderBy(x => x.Date),
            "total" => p.IsDescending ? q.OrderByDescending(x => x.Total) : q.OrderBy(x => x.Total),
            "status" => p.IsDescending ? q.OrderByDescending(x => x.Status) : q.OrderBy(x => x.Status),
            _ => q.OrderByDescending(x => x.CreatedAt),
        };

        var total = await q.CountAsync();
        var data = await q.Skip(p.Skip).Take(p.PerPage)
            .Select(x => ToListDto(x))
            .ToListAsync();

        return PaginatedResponse<PurchaseOrderListDto>.Create(data, total, p.Page, p.PerPage);
    }

    public async Task<PurchaseOrderDto?> GetByIdAsync(Guid id)
    {
        var po = await _db.PurchaseOrders
            .Include(x => x.Supplier)
            .Include(x => x.PurchaseRequest)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id);

        return po is null ? null : ToDto(po);
    }

    public async Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderRequest request)
    {
        var no = await NextNumberAsync();
        var po = new Models.PurchaseOrder
        {
            No = no,
            SupplierId = request.SupplierId,
            PurchaseRequestId = request.PurchaseRequestId,
            Date = request.Date,
            DeliveryDate = request.DeliveryDate,
            Notes = request.Notes,
            Status = PurchaseOrderStatus.Draft,
            Items = request.Items.Select(i => new PurchaseOrderItem
            {
                ItemName = i.ItemName,
                Qty = i.Qty,
                Unit = i.Unit,
                Price = i.Price,
            }).ToList(),
        };

        po.Total = po.Items.Sum(i => i.Qty * i.Price);

        _db.PurchaseOrders.Add(po);
        await _db.SaveChangesAsync();
        return (await GetByIdAsync(po.Id))!;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status)
    {
        var po = await _db.PurchaseOrders.FindAsync(id);
        if (po is null) return false;

        if (!Enum.TryParse<PurchaseOrderStatus>(status, true, out var parsed)) return false;

        po.Status = parsed;
        po.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<PurchaseOrderDto?> ReceiveGoodsAsync(Guid id, ReceiveGoodsRequest request)
    {
        var po = await _db.PurchaseOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (po is null) return null;

        foreach (var recv in request.Items)
        {
            var item = po.Items.FirstOrDefault(i => i.Id == recv.ItemId);
            if (item is not null)
                item.ReceivedQty = Math.Min(item.ReceivedQty + recv.ReceivedQty, item.Qty);
        }

        var allReceived = po.Items.All(i => i.ReceivedQty >= i.Qty);
        var anyReceived = po.Items.Any(i => i.ReceivedQty > 0);

        po.Status = allReceived
            ? PurchaseOrderStatus.Completed
            : anyReceived ? PurchaseOrderStatus.PartialReceive : po.Status;

        po.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var po = await _db.PurchaseOrders.FindAsync(id);
        if (po is null) return false;

        po.IsDeleted = true;
        po.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<string> NextNumberAsync()
    {
        var config = await _db.NumberingConfigs
            .FirstOrDefaultAsync(n => n.DocType == "PURCHASE_ORDER")
            ?? throw new InvalidOperationException("NumberingConfig for PURCHASE_ORDER not found");

        var no = config.GenerateNext();
        await _db.SaveChangesAsync();
        return no;
    }

    private static PurchaseOrderListDto ToListDto(Models.PurchaseOrder x) => new()
    {
        Id = x.Id,
        No = x.No,
        Date = x.Date,
        SupplierName = x.Supplier?.Name ?? string.Empty,
        PurchaseRequestNo = x.PurchaseRequest?.No,
        Status = x.Status.ToString(),
        Total = x.Total,
        DeliveryDate = x.DeliveryDate,
    };

    private static PurchaseOrderDto ToDto(Models.PurchaseOrder x) => new()
    {
        Id = x.Id,
        No = x.No,
        Date = x.Date,
        SupplierName = x.Supplier?.Name ?? string.Empty,
        PurchaseRequestNo = x.PurchaseRequest?.No,
        Status = x.Status.ToString(),
        Total = x.Total,
        DeliveryDate = x.DeliveryDate,
        SupplierId = x.SupplierId,
        PurchaseRequestId = x.PurchaseRequestId,
        Notes = x.Notes,
        CreatedAt = x.CreatedAt,
        Items = x.Items.Select(i => new PurchaseOrderItemDto
        {
            Id = i.Id,
            ItemName = i.ItemName,
            Qty = i.Qty,
            Unit = i.Unit,
            Price = i.Price,
            Total = i.Total,
            ReceivedQty = i.ReceivedQty,
        }).ToList(),
    };
}
