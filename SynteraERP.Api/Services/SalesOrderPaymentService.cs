using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Invoice;
using SynteraERP.Api.DTOs.SalesOrderPayment;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class SalesOrderPaymentService : ISalesOrderPaymentService
{
    private readonly AppDbContext _db;
    private readonly IJournalPostingService _journalPostingService;
    private readonly IInvoiceService _invoiceService;

    public SalesOrderPaymentService(AppDbContext db, IJournalPostingService journalPostingService, IInvoiceService invoiceService)
    {
        _db = db;
        _journalPostingService = journalPostingService;
        _invoiceService = invoiceService;
    }

    public async Task<SalesOrderPaymentDto> RecordDownPaymentAsync(Guid salesOrderId, RecordDownPaymentRequest request)
    {
        if (request.Amount <= 0)
            throw new InvalidOperationException("Jumlah DP harus lebih besar dari 0.");

        if (!Enum.TryParse<PaymentMethod>(request.Method, true, out var method))
            method = PaymentMethod.Transfer;

        await using var tx = await _db.Database.BeginTransactionAsync();

        var so = await _db.SalesOrders
            .Include(x => x.DownPayments)
            .FirstOrDefaultAsync(x => x.Id == salesOrderId && !x.IsDeleted)
            ?? throw new InvalidOperationException("Sales Order tidak ditemukan.");

        // Mirror PurchaseOrderService.RecordPaymentAsync (cap ke po.Total) — beda dari PO, SalesOrder.Total
        // SUDAH termasuk PPN (lihat SalesOrderService.CreateAsync), jadi tidak perlu caveat PPN seperti sisi PO.
        var currentDp = so.DownPayments.Sum(x => x.Amount);
        var newTotal = currentDp + request.Amount;
        if (newTotal > so.Total)
            throw new InvalidOperationException(
                $"Total DP (Rp {newTotal:N0}) melebihi total Sales Order (Rp {so.Total:N0}).");

        var payment = new SalesOrderPayment
        {
            SalesOrderId = salesOrderId,
            PaymentDate = request.PaymentDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Amount = request.Amount,
            Method = method,
            Reference = request.Reference,
            Notes = request.Notes,
        };

        _db.SalesOrderPayments.Add(payment);
        await _db.SaveChangesAsync();

        await _journalPostingService.PostAsync(
            $"Penerimaan DP Sales Order {so.No}",
            JournalSourceType.CustomerAdvanceReceived,
            payment.Id,
            new DateTimeOffset(payment.PaymentDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
            new PostingLine[]
            {
                new("1-1001", request.Amount, 0, "Penerimaan Down Payment dari pelanggan"),
                new("2-4000", 0, request.Amount, "Uang Muka Pelanggan"),
            });

        await tx.CommitAsync();

        return (await ListForSalesOrderAsync(salesOrderId)).First(x => x.Id == payment.Id);
    }

    public async Task<List<SalesOrderPaymentDto>> ListForSalesOrderAsync(Guid salesOrderId)
    {
        var payments = await _db.SalesOrderPayments
            .Include(x => x.Applications)
            .Where(x => x.SalesOrderId == salesOrderId)
            .OrderBy(x => x.PaymentDate)
            .ToListAsync();

        return payments.Select(ToDto).ToList();
    }

    public async Task<InvoiceDto?> ApplyToInvoiceAsync(Guid invoiceId, ApplyDownPaymentRequest request)
    {
        if (request.AmountToApply <= 0)
            throw new InvalidOperationException("Jumlah yang diterapkan harus lebih besar dari 0.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var invoice = await _db.Invoices.FirstOrDefaultAsync(x => x.Id == invoiceId);
        if (invoice is null) return null;

        var dp = await _db.SalesOrderPayments
            .Include(x => x.Applications)
            .FirstOrDefaultAsync(x => x.Id == request.SalesOrderPaymentId)
            ?? throw new InvalidOperationException("Down Payment tidak ditemukan.");

        if (dp.SalesOrderId != invoice.SalesOrderId)
            throw new InvalidOperationException(
                "Down Payment ini bukan milik Sales Order yang terhubung ke Invoice ini.");

        var remainingDp = dp.Amount - dp.Applications.Sum(a => a.AmountApplied);
        if (request.AmountToApply > remainingDp)
            throw new InvalidOperationException(
                $"Jumlah yang diterapkan (Rp {request.AmountToApply:N0}) melebihi sisa DP yang belum diterapkan (Rp {remainingDp:N0}).");

        if (request.AmountToApply > invoice.Balance)
            throw new InvalidOperationException(
                $"Jumlah yang diterapkan (Rp {request.AmountToApply:N0}) melebihi sisa tagihan Invoice (Rp {invoice.Balance:N0}).");

        _db.DownPaymentApplications.Add(new DownPaymentApplication
        {
            SalesOrderPaymentId = dp.Id,
            InvoiceId = invoice.Id,
            AmountApplied = request.AmountToApply,
        });

        // Reuse Invoice.ApplyPayment — satu-satunya tempat status Paid/PartialPaid dihitung, dipakai
        // sama oleh InvoiceService.RecordPaymentAsync, supaya jalur pelunasan biasa dan jalur DP tidak
        // pernah punya logic transisi status yang beda.
        invoice.ApplyPayment(request.AmountToApply);
        invoice.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

        await _journalPostingService.PostAsync(
            $"Penerapan DP ke Invoice {invoice.No}",
            JournalSourceType.CustomerAdvanceApplied,
            dp.Id,
            DateTimeOffset.UtcNow,
            new PostingLine[]
            {
                new("2-4000", request.AmountToApply, 0, "Reklas Uang Muka Pelanggan"),
                new("1-2000", 0, request.AmountToApply, "Pelunasan Piutang Usaha dari DP"),
            });

        await tx.CommitAsync();

        return await _invoiceService.GetByIdAsync(invoice.Id);
    }

    private static SalesOrderPaymentDto ToDto(SalesOrderPayment x)
    {
        var applied = x.Applications.Sum(a => a.AmountApplied);
        return new SalesOrderPaymentDto
        {
            Id = x.Id,
            SalesOrderId = x.SalesOrderId,
            PaymentDate = x.PaymentDate,
            Amount = x.Amount,
            Method = x.Method.ToString(),
            Reference = x.Reference,
            Notes = x.Notes,
            AmountApplied = applied,
            Remaining = x.Amount - applied,
            CreatedAt = x.CreatedAt,
        };
    }
}
