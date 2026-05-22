using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Invoice;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _db;

    public InvoiceService(AppDbContext db) => _db = db;

    public async Task<PaginatedResponse<InvoiceListDto>> ListAsync(PaginationParams p)
    {
        var q = _db.Invoices
            .Include(x => x.Customer)
            .Include(x => x.SalesOrder)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(p.Search))
        {
            var s = p.Search.ToLower();
            q = q.Where(x => x.No.ToLower().Contains(s) || x.Customer.Name.ToLower().Contains(s));
        }

        q = p.SortBy switch
        {
            "no" => p.IsDescending ? q.OrderByDescending(x => x.No) : q.OrderBy(x => x.No),
            "date" => p.IsDescending ? q.OrderByDescending(x => x.InvoiceDate) : q.OrderBy(x => x.InvoiceDate),
            "dueDate" => p.IsDescending ? q.OrderByDescending(x => x.DueDate) : q.OrderBy(x => x.DueDate),
            "amount" => p.IsDescending ? q.OrderByDescending(x => x.Amount) : q.OrderBy(x => x.Amount),
            "status" => p.IsDescending ? q.OrderByDescending(x => x.Status) : q.OrderBy(x => x.Status),
            _ => q.OrderByDescending(x => x.CreatedAt),
        };

        var total = await q.CountAsync();
        var data = await q.Skip(p.Skip).Take(p.PerPage)
            .Select(x => ToListDto(x))
            .ToListAsync();

        return PaginatedResponse<InvoiceListDto>.Create(data, total, p.Page, p.PerPage);
    }

    public async Task<InvoiceDto?> GetByIdAsync(Guid id)
    {
        var inv = await _db.Invoices
            .Include(x => x.Customer)
            .Include(x => x.SalesOrder)
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == id);

        return inv is null ? null : ToDto(inv);
    }

    public async Task<InvoiceDto> CreateAsync(CreateInvoiceRequest request)
    {
        var no = await NextNumberAsync();
        var inv = new Models.Invoice
        {
            No = no,
            CustomerId = request.CustomerId,
            SalesOrderId = request.SalesOrderId,
            InvoiceDate = request.Date,
            DueDate = request.DueDate,
            Amount = request.Amount,
            Paid = 0,
            Notes = request.Notes,
            Status = InvoiceStatus.Draft,
        };

        _db.Invoices.Add(inv);
        await _db.SaveChangesAsync();
        return (await GetByIdAsync(inv.Id))!;
    }

    public async Task<InvoiceDto?> RecordPaymentAsync(Guid id, RecordPaymentRequest request)
    {
        var inv = await _db.Invoices
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (inv is null) return null;

        if (!Enum.TryParse<PaymentMethod>(request.Method, true, out var method))
            method = PaymentMethod.Transfer;

        var payment = new Payment
        {
            InvoiceId = id,
            PaymentDate = request.Date,
            Amount = request.Amount,
            Method = method,
            Reference = request.Reference,
            Notes = request.Notes,
        };

        _db.Payments.Add(payment);
        inv.Paid += request.Amount;
        inv.UpdatedAt = DateTimeOffset.UtcNow;

        inv.Status = inv.Paid >= inv.Amount
            ? InvoiceStatus.Paid
            : InvoiceStatus.PartialPaid;

        await _db.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var inv = await _db.Invoices.FindAsync(id);
        if (inv is null) return false;

        inv.IsDeleted = true;
        inv.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<string> NextNumberAsync()
    {
        var config = await _db.NumberingConfigs
            .FirstOrDefaultAsync(n => n.DocType == "INVOICE")
            ?? throw new InvalidOperationException("NumberingConfig for INVOICE not found");

        var no = config.GenerateNext();
        await _db.SaveChangesAsync();
        return no;
    }

    private static InvoiceListDto ToListDto(Models.Invoice x) => new()
    {
        Id = x.Id,
        No = x.No,
        Date = x.InvoiceDate,
        DueDate = x.DueDate,
        CustomerName = x.Customer?.Name ?? string.Empty,
        SalesOrderNo = x.SalesOrder?.No,
        Status = x.Status.ToString(),
        Amount = x.Amount,
        Paid = x.Paid,
        Balance = x.Balance,
    };

    private static InvoiceDto ToDto(Models.Invoice x) => new()
    {
        Id = x.Id,
        No = x.No,
        Date = x.InvoiceDate,
        DueDate = x.DueDate,
        CustomerName = x.Customer?.Name ?? string.Empty,
        SalesOrderNo = x.SalesOrder?.No,
        Status = x.Status.ToString(),
        Amount = x.Amount,
        Paid = x.Paid,
        Balance = x.Balance,
        CustomerId = x.CustomerId,
        SalesOrderId = x.SalesOrderId,
        Notes = x.Notes,
        CreatedAt = x.CreatedAt,
        Payments = x.Payments.OrderBy(p => p.PaymentDate).Select(p => new PaymentDto
        {
            Id = p.Id,
            Date = p.PaymentDate,
            Amount = p.Amount,
            Method = p.Method.ToString(),
            Reference = p.Reference,
            Notes = p.Notes,
        }).ToList(),
    };
}
