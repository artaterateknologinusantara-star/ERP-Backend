using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.SalesOrder;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class SalesOrderService : ISalesOrderService
{
    private readonly AppDbContext _db;

    public SalesOrderService(AppDbContext db) => _db = db;

    public async Task<PaginatedResponse<SalesOrderListDto>> ListAsync(PaginationParams p)
    {
        var q = _db.SalesOrders
            .Include(x => x.Customer)
            .Include(x => x.Sales)
            .Include(x => x.Quotation)
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
            "total" => p.IsDescending ? q.OrderByDescending(x => x.Total) : q.OrderBy(x => x.Total),
            "status" => p.IsDescending ? q.OrderByDescending(x => x.Status) : q.OrderBy(x => x.Status),
            _ => q.OrderByDescending(x => x.CreatedAt),
        };

        var total = await q.CountAsync();
        var data = await q.Skip(p.Skip).Take(p.PerPage)
            .Select(x => ToListDto(x))
            .ToListAsync();

        return PaginatedResponse<SalesOrderListDto>.Create(data, total, p.Page, p.PerPage);
    }

    public async Task<SalesOrderDto?> GetByIdAsync(Guid id)
    {
        var so = await _db.SalesOrders
            .Include(x => x.Customer)
            .Include(x => x.Sales)
            .Include(x => x.Quotation)
            .FirstOrDefaultAsync(x => x.Id == id);

        return so is null ? null : ToDto(so);
    }

    public async Task<SalesOrderDto> CreateAsync(CreateSalesOrderRequest request)
    {
        if (request.QuotationId.HasValue)
        {
            var quotation = await _db.Quotations.FindAsync(request.QuotationId.Value)
                ?? throw new KeyNotFoundException("Quotation tidak ditemukan.");

            if (quotation.Status != QuotationStatus.Disetujui)
                throw new InvalidOperationException("SO hanya dapat dibuat dari Quotation yang sudah Disetujui.");

            var hasCpo = await _db.CustomerPOs.AnyAsync(c => c.QuotationId == request.QuotationId.Value);
            if (!hasCpo)
                throw new InvalidOperationException("Customer PO belum diinput untuk Quotation ini.");
        }

        var no = await NextNumberAsync();
        var so = new Models.SalesOrder
        {
            No = no,
            CustomerId = request.CustomerId,
            SalesId = request.SalesId,
            QuotationId = request.QuotationId,
            ProjectName = request.ProjectName,
            Date = request.Date,
            DeliveryDate = request.DeliveryDate,
            Total = request.Total,
            Notes = request.Notes,
            Status = SalesOrderStatus.Open,
        };

        _db.SalesOrders.Add(so);
        await _db.SaveChangesAsync();
        return (await GetByIdAsync(so.Id))!;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status)
    {
        var so = await _db.SalesOrders.FindAsync(id);
        if (so is null) return false;

        if (!Enum.TryParse<SalesOrderStatus>(status, true, out var parsed)) return false;

        so.Status = parsed;
        so.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var so = await _db.SalesOrders.FindAsync(id);
        if (so is null) return false;

        so.IsDeleted = true;
        so.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return true;
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

    private static SalesOrderListDto ToListDto(Models.SalesOrder x) => new()
    {
        Id = x.Id,
        No = x.No,
        Date = x.Date,
        ProjectName = x.ProjectName,
        CustomerName = x.Customer?.Name ?? string.Empty,
        SalesName = x.Sales?.Name ?? string.Empty,
        Status = x.Status.ToString(),
        Total = x.Total,
        QuotationNo = x.Quotation?.No,
    };

    private static SalesOrderDto ToDto(Models.SalesOrder x) => new()
    {
        Id = x.Id,
        No = x.No,
        Date = x.Date,
        ProjectName = x.ProjectName,
        CustomerName = x.Customer?.Name ?? string.Empty,
        SalesName = x.Sales?.Name ?? string.Empty,
        Status = x.Status.ToString(),
        Total = x.Total,
        QuotationNo = x.Quotation?.No,
        CustomerId = x.CustomerId,
        SalesId = x.SalesId,
        QuotationId = x.QuotationId,
        DeliveryDate = x.DeliveryDate,
        Notes = x.Notes,
        CreatedAt = x.CreatedAt,
    };
}
