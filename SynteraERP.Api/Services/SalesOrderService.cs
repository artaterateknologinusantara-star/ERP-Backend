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

    public async Task<PaginatedResponse<SalesOrderListResponse>> GetListAsync(int page, int perPage, string? search, string? status)
    {
        var q = _db.SalesOrders
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

        return PaginatedResponse<SalesOrderListResponse>.Create(data, total, page, perPage);
    }

    public async Task<SalesOrderDetailResponse?> GetByIdAsync(Guid id)
    {
        var so = await _db.SalesOrders
            .Include(x => x.Customer)
            .Include(x => x.Sales)
            .Include(x => x.Items.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        return so is null ? null : ToDetailResponse(so);
    }

    public async Task<SalesOrderDetailResponse> CreateAsync(CreateSalesOrderRequest request)
    {
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

        var subTotal = Math.Round(items.Sum(x => x.Amount), 2);
        var taxAmount = Math.Round(subTotal * 11 / 100, 2);
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
            Items = items,
        };

        _db.SalesOrders.Add(so);
        _db.Projects.Add(await BuildProjectForSoAsync(so, grandTotal));
        await _db.SaveChangesAsync();

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

        var subTotal = Math.Round(soItems.Sum(x => x.Amount), 2);
        var taxAmount = Math.Round(subTotal * 11 / 100, 2);
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

        _db.SalesOrders.Add(so);
        _db.Projects.Add(await BuildProjectForSoAsync(so, grandTotal));

        if (quotation.Status == QuotationStatus.Disetujui)
            quotation.Status = QuotationStatus.Selesai;

        await _db.SaveChangesAsync();

        return (await GetByIdAsync(so.Id))!;
    }

    private async Task<Project> BuildProjectForSoAsync(SalesOrder so, decimal budget)
    {
        var year  = DateTime.UtcNow.Year;
        var count = await _db.Projects.CountAsync() + 1;
        var code  = $"PRJ-{year}-{count:D3}";
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

    private static SalesOrderDetailResponse ToDetailResponse(SalesOrder so)
    {
        var subTotal = so.Items.Any()
            ? Math.Round(so.Items.Sum(x => x.Amount), 2)
            : Math.Round(so.Total / 1.11m, 2);

        var taxAmount = Math.Round(subTotal * 11 / 100, 2);
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
            SubTotal = subTotal,
            TaxAmount = taxAmount,
            GrandTotal = grandTotal,
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
