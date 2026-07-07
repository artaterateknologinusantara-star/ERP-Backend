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
        // Auto-update overdue status
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var overdueItems = await _db.Invoices
            .Where(x => !x.IsDeleted
                && x.Status != InvoiceStatus.Paid
                && x.DueDate < today
                && x.Status != InvoiceStatus.Overdue)
            .ToListAsync();
        if (overdueItems.Any())
        {
            overdueItems.ForEach(x => x.Status = InvoiceStatus.Overdue);
            await _db.SaveChangesAsync();
        }

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
            .ToListAsync();

        var todayForAging = DateOnly.FromDateTime(DateTime.UtcNow);
        var mapped = data.Select(x => ToListDto(x, todayForAging)).ToList();

        return PaginatedResponse<InvoiceListDto>.Create(mapped, total, p.Page, p.PerPage);
    }

    public async Task<InvoiceDto?> GetByIdAsync(Guid id)
    {
        var inv = await _db.Invoices
            .Include(x => x.Customer)
            .Include(x => x.SalesOrder)
            .Include(x => x.Payments)
            .Include(x => x.Items.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(x => x.Id == id);

        return inv is null ? null : ToDto(inv);
    }

    public async Task<InvoiceDto> CreateAsync(CreateInvoiceRequest request)
    {
        var no = await NextNumberAsync();
        var inv = new Models.Invoice
        {
            No           = no,
            CustomerId   = request.CustomerId,
            SalesOrderId = request.SalesOrderId,
            InvoiceDate  = request.Date,
            DueDate      = request.DueDate,
            Amount       = request.Amount,
            Paid         = 0,
            Notes        = request.Notes,
            Terms        = request.Terms,
            Status       = InvoiceStatus.Draft,
        };

        // Auto-populate items dari SalesOrderItems jika SO ada
        if (request.SalesOrderId.HasValue)
        {
            var soItems = await _db.SalesOrderItems
                .Where(x => x.SalesOrderId == request.SalesOrderId.Value)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();

            if (soItems.Any())
            {
                inv.Items = soItems.Select((item, index) => new InvoiceItem
                {
                    Id          = Guid.NewGuid(),
                    InvoiceId   = inv.Id,           // explicit FK
                    Description = item.Description,
                    Sku         = item.Sku,
                    Qty         = item.Qty,
                    Uom         = item.Uom ?? string.Empty,
                    UnitPrice   = item.UnitPrice,
                    Amount      = item.Amount,
                    SortOrder   = index,
                }).ToList();

                // Recalculate Amount dari items
                var subTotal  = Math.Round(inv.Items.Sum(x => x.Amount), 2);
                var taxAmount = Math.Round(subTotal * 0.11m, 2);
                inv.Amount    = subTotal + taxAmount;
            }
        }

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

    public async Task<InvoiceStatsResponse> GetStatsAsync()
    {
        return new InvoiceStatsResponse
        {
            Total = await _db.Invoices.CountAsync(x => !x.IsDeleted),
            Outstanding = await _db.Invoices.CountAsync(x => !x.IsDeleted
                && (x.Status == InvoiceStatus.Sent || x.Status == InvoiceStatus.PartialPaid)),
            Overdue = await _db.Invoices.CountAsync(x => !x.IsDeleted
                && x.Status == InvoiceStatus.Overdue),
            TotalCollected = await _db.Invoices
                .Where(x => !x.IsDeleted)
                .SumAsync(x => x.Paid),
        };
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

    private static InvoiceListDto ToListDto(Models.Invoice x, DateOnly today) => new()
    {
        Id = x.Id,
        No = x.No,
        InvoiceDate = x.InvoiceDate,
        DueDate = x.DueDate,
        CustomerName = x.Customer?.Name ?? string.Empty,
        SalesOrderNo = x.SalesOrder?.No,
        Status = x.Status == InvoiceStatus.PartialPaid ? "Partial Paid" : x.Status.ToString(),
        Amount = x.Amount,
        Paid = x.Paid,
        Balance = x.Balance,
        AgingDays = (x.Status == InvoiceStatus.Overdue && x.DueDate < today)
            ? (today.DayNumber - x.DueDate.DayNumber)
            : 0,
    };

    private static InvoiceDto ToDto(Models.Invoice x)
    {
        // Compute subTotal / taxAmount dari items jika ada, otherwise reverse dari Amount
        var hasItems  = x.Items != null && x.Items.Any();
        var subTotal  = hasItems
            ? Math.Round(x.Items!.Sum(i => i.Amount), 2)
            : Math.Round(x.Amount / 1.11m, 2);
        var taxAmount = hasItems
            ? Math.Round(subTotal * 0.11m, 2)
            : x.Amount - subTotal;

        return new InvoiceDto
        {
            Id           = x.Id,
            No           = x.No,
            InvoiceDate  = x.InvoiceDate,
            DueDate      = x.DueDate,
            CustomerName = x.Customer?.Name ?? string.Empty,
            SalesOrderNo = x.SalesOrder?.No,
            Status       = x.Status == InvoiceStatus.PartialPaid ? "Partial Paid" : x.Status.ToString(),
            Amount       = x.Amount,
            Paid         = x.Paid,
            Balance      = x.Balance,
            CustomerId   = x.CustomerId,
            SalesOrderId = x.SalesOrderId,
            Notes        = x.Notes,
            Terms        = x.Terms,
            SubTotal     = subTotal,
            TaxAmount    = taxAmount,
            CreatedAt    = x.CreatedAt,
            Payments     = x.Payments.OrderBy(p => p.PaymentDate).Select(p => new PaymentDto
            {
                Id          = p.Id,
                PaymentDate = p.PaymentDate,
                Amount      = p.Amount,
                Method      = p.Method.ToString(),
                Reference   = p.Reference,
                Notes       = p.Notes,
            }).ToList(),
            Items = (x.Items ?? []).OrderBy(i => i.SortOrder).Select(i => new InvoiceItemResponse
            {
                Id          = i.Id,
                Description = i.Description,
                Sku         = i.Sku,
                Qty         = i.Qty,
                Uom         = i.Uom,
                UnitPrice   = i.UnitPrice,
                Amount      = i.Amount,
                SortOrder   = i.SortOrder,
            }).ToList(),
        };
    }
}
