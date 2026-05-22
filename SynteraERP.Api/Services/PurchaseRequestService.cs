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
            .Include(x => x.RequestedByUser)
            .Include(x => x.SalesOrder)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(p.Search))
        {
            var s = p.Search.ToLower();
            q = q.Where(x => x.No.ToLower().Contains(s) || x.RequestedByUser.Name.ToLower().Contains(s));
        }

        q = p.SortBy switch
        {
            "no" => p.IsDescending ? q.OrderByDescending(x => x.No) : q.OrderBy(x => x.No),
            "date" => p.IsDescending ? q.OrderByDescending(x => x.Date) : q.OrderBy(x => x.Date),
            "status" => p.IsDescending ? q.OrderByDescending(x => x.Status) : q.OrderBy(x => x.Status),
            _ => q.OrderByDescending(x => x.CreatedAt),
        };

        var total = await q.CountAsync();
        var data = await q.Skip(p.Skip).Take(p.PerPage)
            .Select(x => ToListDto(x))
            .ToListAsync();

        return PaginatedResponse<PurchaseRequestListDto>.Create(data, total, p.Page, p.PerPage);
    }

    public async Task<PurchaseRequestDto?> GetByIdAsync(Guid id)
    {
        var pr = await _db.PurchaseRequests
            .Include(x => x.RequestedByUser)
            .Include(x => x.SalesOrder)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id);

        return pr is null ? null : ToDto(pr);
    }

    public async Task<PurchaseRequestDto> CreateAsync(CreatePurchaseRequestRequest request)
    {
        var no = await NextNumberAsync();
        var pr = new Models.PurchaseRequest
        {
            No = no,
            RequestedBy = request.RequestedBy,
            SalesOrderId = request.SalesOrderId,
            Date = request.Date,
            Notes = request.Notes,
            Status = PurchaseRequestStatus.Draft,
            Items = request.Items.Select(i => new PurchaseRequestItem
            {
                ItemName = i.ItemName,
                Qty = i.Qty,
                Unit = i.Unit,
                EstPrice = i.EstPrice,
                Notes = i.Notes,
            }).ToList(),
        };

        pr.Total = pr.Items.Sum(i => i.Qty * i.EstPrice);

        _db.PurchaseRequests.Add(pr);
        await _db.SaveChangesAsync();
        return (await GetByIdAsync(pr.Id))!;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status)
    {
        var pr = await _db.PurchaseRequests.FindAsync(id);
        if (pr is null) return false;

        if (!Enum.TryParse<PurchaseRequestStatus>(status, true, out var parsed)) return false;

        pr.Status = parsed;
        pr.UpdatedAt = DateTimeOffset.UtcNow;
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
        Id = x.Id,
        No = x.No,
        Date = x.Date,
        RequestedByName = x.RequestedByUser?.Name ?? string.Empty,
        SalesOrderNo = x.SalesOrder?.No,
        Status = x.Status.ToString(),
        Total = x.Total,
        Notes = x.Notes,
    };

    private static PurchaseRequestDto ToDto(Models.PurchaseRequest x) => new()
    {
        Id = x.Id,
        No = x.No,
        Date = x.Date,
        RequestedByName = x.RequestedByUser?.Name ?? string.Empty,
        SalesOrderNo = x.SalesOrder?.No,
        Status = x.Status.ToString(),
        Total = x.Total,
        Notes = x.Notes,
        RequestedBy = x.RequestedBy,
        SalesOrderId = x.SalesOrderId,
        CreatedAt = x.CreatedAt,
        Items = x.Items.Select(i => new PurchaseRequestItemDto
        {
            Id = i.Id,
            ItemName = i.ItemName,
            Qty = i.Qty,
            Unit = i.Unit,
            EstPrice = i.EstPrice,
            Notes = i.Notes,
        }).ToList(),
    };
}
