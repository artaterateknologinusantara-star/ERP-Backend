using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.Models;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/finance")]
public class FinanceController(AppDbContext db) : ControllerBase
{
    private static int CalcAgingDays(DateOnly dueDate, DateOnly today) =>
        Math.Max(0, today.DayNumber - dueDate.DayNumber);

    private static string AgingBucket(int days) => days switch
    {
        <= 0  => "Current",
        <= 30 => "1-30 hari",
        <= 60 => "31-60 hari",
        <= 90 => "61-90 hari",
        _     => ">90 hari",
    };

    // ── GET /api/finance/ar ───────────────────────────────────────────────────
    [HttpGet("ar")]
    public async Task<IActionResult> GetAR()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var invoices = await db.Invoices
            .Include(x => x.Customer)
            .Include(x => x.SalesOrder)
            .Where(x => x.Amount > x.Paid)
            .OrderBy(x => x.DueDate)
            .ToListAsync();

        var result = invoices.Select(x =>
        {
            int days    = CalcAgingDays(x.DueDate, today);
            decimal bal = x.Amount - x.Paid;
            return new
            {
                id           = x.Id,
                invoiceNo    = x.No,
                customerId   = x.CustomerId,
                customerName = x.Customer.Name,
                salesOrderNo = x.SalesOrder?.No,
                invoiceDate  = x.InvoiceDate,
                dueDate      = x.DueDate,
                terms        = x.Terms,
                grandTotal   = x.Amount,
                paid         = x.Paid,
                balance      = bal,
                status       = x.Status.ToString(),
                agingDays    = days,
                agingBucket  = AgingBucket(days),
            };
        }).ToList();

        return Ok(new { success = true, data = result });
    }

    // ── GET /api/finance/ar/stats ─────────────────────────────────────────────
    [HttpGet("ar/stats")]
    public async Task<IActionResult> GetARStats()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var invoices = await db.Invoices
            .Where(x => x.Amount > x.Paid)
            .ToListAsync();

        var items = invoices.Select(x => new
        {
            balance   = x.Amount - x.Paid,
            agingDays = CalcAgingDays(x.DueDate, today),
        }).ToList();

        return Ok(new
        {
            success = true,
            data = new
            {
                totalOutstanding = items.Sum(x => x.balance),
                current          = items.Where(x => x.agingDays <= 0).Sum(x => x.balance),
                overdue1to30     = items.Where(x => x.agingDays is >= 1 and <= 30).Sum(x => x.balance),
                overdue31to60    = items.Where(x => x.agingDays is >= 31 and <= 60).Sum(x => x.balance),
                overdue61to90    = items.Where(x => x.agingDays is >= 61 and <= 90).Sum(x => x.balance),
                overdueOver90    = items.Where(x => x.agingDays > 90).Sum(x => x.balance),
                totalInvoices    = items.Count,
                overdueCount     = items.Count(x => x.agingDays > 0),
            }
        });
    }

    // ── GET /api/finance/ap ───────────────────────────────────────────────────
    [HttpGet("ap")]
    public async Task<IActionResult> GetAP()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var pos = await db.PurchaseOrders
            .Include(x => x.Supplier)
            .Include(x => x.PurchaseRequest)
            .Include(x => x.Payments)
            .Where(x => x.Status == PurchaseOrderStatus.Ordered ||
                        x.Status == PurchaseOrderStatus.PartialReceive ||
                        x.Status == PurchaseOrderStatus.Completed)
            .OrderBy(x => x.Date)
            .ToListAsync();

        var result = pos.Select(x =>
        {
            var totalPaid = x.Payments.Sum(p => p.Amount);
            return new
            {
                id                = x.Id,
                poNo              = x.No,
                supplierId        = x.SupplierId,
                supplierName      = x.Supplier.Name,
                purchaseRequestNo = x.PurchaseRequest?.No,
                poDate            = x.Date,
                deliveryDate      = x.DeliveryDate,
                total             = x.Total,
                totalPaid         = totalPaid,
                balance           = x.Total - totalPaid,
                status            = x.Status.ToString(),
                agingDays         = Math.Max(0, today.DayNumber - x.Date.DayNumber),
            };
        }).ToList();

        return Ok(new { success = true, data = result });
    }

    // ── GET /api/finance/ap/stats ─────────────────────────────────────────────
    [HttpGet("ap/stats")]
    public async Task<IActionResult> GetAPStats()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var pos = await db.PurchaseOrders
            .Where(x => x.Status == PurchaseOrderStatus.Ordered ||
                        x.Status == PurchaseOrderStatus.PartialReceive ||
                        x.Status == PurchaseOrderStatus.Completed)
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                totalPending    = pos.Sum(x => x.Total),
                ordered         = pos.Where(x => x.Status == PurchaseOrderStatus.Ordered).Sum(x => x.Total),
                partialReceive  = pos.Where(x => x.Status == PurchaseOrderStatus.PartialReceive).Sum(x => x.Total),
                completed       = pos.Where(x => x.Status == PurchaseOrderStatus.Completed).Sum(x => x.Total),
                totalPos        = pos.Count,
                overdueDelivery = pos.Count(x => x.DeliveryDate.HasValue && x.DeliveryDate.Value < today),
            }
        });
    }

    // ── GET /api/finance/cash-in ──────────────────────────────────────────────
    [HttpGet("cash-in")]
    public async Task<IActionResult> GetCashIn(
        [FromQuery] string? startDate,
        [FromQuery] string? endDate)
    {
        var query = db.Payments
            .Include(x => x.Invoice)
                .ThenInclude(inv => inv.Customer)
            .Where(x => !x.Invoice.IsDeleted)
            .AsQueryable();

        if (DateOnly.TryParse(startDate, out var sd))
            query = query.Where(x => x.PaymentDate >= sd);
        if (DateOnly.TryParse(endDate, out var ed))
            query = query.Where(x => x.PaymentDate <= ed);

        var payments = await query
            .OrderByDescending(x => x.PaymentDate)
            .ToListAsync();

        var result = payments.Select(x => new
        {
            id           = x.Id,
            date         = x.PaymentDate,
            refNo        = x.Reference ?? x.Invoice.No,
            description  = $"Pembayaran Invoice {x.Invoice.No}",
            customerName = x.Invoice.Customer.Name,
            invoiceNo    = x.Invoice.No,
            invoiceId    = x.InvoiceId,
            amount       = x.Amount,
            method       = x.Method.ToString(),
            notes        = x.Notes,
        }).ToList();

        return Ok(new { success = true, data = result });
    }

    // ── GET /api/finance/cash-out ─────────────────────────────────────────────
    [HttpGet("cash-out")]
    public async Task<IActionResult> GetCashOut(
        [FromQuery] string? startDate,
        [FromQuery] string? endDate)
    {
        var query = db.POPayments
            .Include(x => x.PurchaseOrder)
                .ThenInclude(po => po.Supplier)
            .AsQueryable();

        if (DateOnly.TryParse(startDate, out var sd))
            query = query.Where(x => x.PaymentDate >= sd);
        if (DateOnly.TryParse(endDate, out var ed))
            query = query.Where(x => x.PaymentDate <= ed);

        var payments = await query
            .OrderByDescending(x => x.PaymentDate)
            .ToListAsync();

        var result = payments.Select(p => new
        {
            id           = p.Id,
            date         = p.PaymentDate,
            refNo        = p.Reference ?? p.PurchaseOrder.No,
            description  = $"Pembayaran PO {p.PurchaseOrder.No} ke {p.PurchaseOrder.Supplier.Name}",
            supplierName = p.PurchaseOrder.Supplier.Name,
            poNo         = p.PurchaseOrder.No,
            amount       = p.Amount,
            method       = p.Method,
            notes        = p.Notes,
        }).ToList();

        return Ok(new { success = true, data = result });
    }

    // ── GET /api/finance/summary ──────────────────────────────────────────────
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var today        = DateOnly.FromDateTime(DateTime.UtcNow);
        var firstOfMonth = new DateOnly(today.Year, today.Month, 1);
        var firstOfYear  = new DateOnly(today.Year, 1, 1);

        var payments = await db.Payments
            .Where(x => !x.Invoice.IsDeleted)
            .ToListAsync();

        var poPayments = await db.POPayments.ToListAsync();

        var pos = await db.PurchaseOrders
            .Where(x => x.Status != PurchaseOrderStatus.Draft &&
                        x.Status != PurchaseOrderStatus.Cancelled)
            .ToListAsync();

        var invoices = await db.Invoices
            .Where(x => x.Amount > x.Paid)
            .ToListAsync();

        var posAP = pos.Where(x =>
            x.Status == PurchaseOrderStatus.Ordered ||
            x.Status == PurchaseOrderStatus.PartialReceive).ToList();

        var ciAll   = payments.Sum(x => x.Amount);
        var ciMonth = payments.Where(x => x.PaymentDate >= firstOfMonth).Sum(x => x.Amount);
        var ciYear  = payments.Where(x => x.PaymentDate >= firstOfYear).Sum(x => x.Amount);
        var coAll   = poPayments.Sum(x => x.Amount);
        var coMonth = poPayments.Where(x => x.PaymentDate >= firstOfMonth).Sum(x => x.Amount);
        var coYear  = poPayments.Where(x => x.PaymentDate >= firstOfYear).Sum(x => x.Amount);

        return Ok(new
        {
            success = true,
            data = new
            {
                totalCashInAllTime    = ciAll,
                totalCashInThisMonth  = ciMonth,
                totalCashInThisYear   = ciYear,
                totalCashOutAllTime   = coAll,
                totalCashOutThisMonth = coMonth,
                totalCashOutThisYear  = coYear,
                totalAR               = invoices.Sum(x => x.Amount - x.Paid),
                overdueAR             = invoices.Where(x => x.DueDate < today).Sum(x => x.Amount - x.Paid),
                totalAP               = posAP.Sum(x => x.Total),
                netCashThisMonth      = ciMonth - coMonth,
                netCashThisYear       = ciYear  - coYear,
            }
        });
    }

    // ── GET /api/finance/monthly-chart ────────────────────────────────────────
    [HttpGet("monthly-chart")]
    public async Task<IActionResult> GetMonthlyChart()
    {
        var months = Enumerable.Range(0, 6)
            .Select(i => DateTime.UtcNow.AddMonths(-5 + i))
            .Select(d => new DateOnly(d.Year, d.Month, 1))
            .ToList();

        var minDate = months[0];

        var payments = await db.Payments
            .Where(x => !x.Invoice.IsDeleted && x.PaymentDate >= minDate)
            .ToListAsync();

        var poPayments = await db.POPayments
            .Where(x => x.PaymentDate >= minDate)
            .ToListAsync();

        string[] indoNames = ["", "Jan", "Feb", "Mar", "Apr", "Mei", "Jun", "Jul", "Agu", "Sep", "Okt", "Nov", "Des"];

        var result = months.Select(m =>
        {
            var ci = payments
                .Where(x => x.PaymentDate.Year == m.Year && x.PaymentDate.Month == m.Month)
                .Sum(x => x.Amount);
            var co = poPayments
                .Where(x => x.PaymentDate.Year == m.Year && x.PaymentDate.Month == m.Month)
                .Sum(x => x.Amount);
            return new
            {
                month   = indoNames[m.Month],
                year    = m.Year,
                cashIn  = ci,
                cashOut = co,
                net     = ci - co,
            };
        }).ToList();

        return Ok(new { success = true, data = result });
    }
}
