using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Purchasing;
using SynteraERP.Api.DTOs.SupplierInvoice;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class SupplierInvoiceService : ISupplierInvoiceService
{
    private readonly AppDbContext _db;
    private readonly IJournalPostingService _journalPostingService;
    private readonly IPurchaseOrderService _purchaseOrderService;

    public SupplierInvoiceService(AppDbContext db, IJournalPostingService journalPostingService, IPurchaseOrderService purchaseOrderService)
    {
        _db = db;
        _journalPostingService = journalPostingService;
        _purchaseOrderService = purchaseOrderService;
    }

    public async Task<PaginatedResponse<SupplierInvoiceListDto>> ListAsync(SupplierInvoiceQueryParams p)
    {
        var q = _db.SupplierInvoices
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.PurchaseOrder)
            .Include(x => x.Payments)
                .ThenInclude(pay => pay.POPayment)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(p.Status) && Enum.TryParse<SupplierInvoiceStatus>(p.Status, true, out var status))
            q = q.Where(x => x.Status == status);

        if (p.SupplierId.HasValue)
            q = q.Where(x => x.SupplierId == p.SupplierId.Value);

        if (p.PurchaseOrderId.HasValue)
            q = q.Where(x => x.PurchaseOrderId == p.PurchaseOrderId.Value);

        q = q.OrderByDescending(x => x.CreatedAt);

        var total = await q.CountAsync();
        var data = await q.Skip(p.Skip).Take(p.PerPage).ToListAsync();
        var mapped = data.Select(ToListDto).ToList();

        return PaginatedResponse<SupplierInvoiceListDto>.Create(mapped, total, p.Page, p.PerPage);
    }

    public async Task<SupplierInvoiceDto?> GetByIdAsync(Guid id)
    {
        var inv = await _db.SupplierInvoices
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.PurchaseOrder)
            .Include(x => x.Payments)
                .ThenInclude(pay => pay.POPayment)
            .Include(x => x.Items)
                .ThenInclude(i => i.PurchaseOrderItem)
            .FirstOrDefaultAsync(x => x.Id == id);

        return inv is null ? null : ToDto(inv);
    }

    public async Task<SupplierInvoiceDto> CreateAsync(CreateSupplierInvoiceRequest request)
    {
        if (request.Items.Count == 0)
            throw new ArgumentException("Supplier invoice harus punya minimal 1 baris item.");

        var po = await _db.PurchaseOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == request.PurchaseOrderId && !x.IsDeleted)
            ?? throw new KeyNotFoundException("Purchase Order tidak ditemukan.");

        if (po.Status != PurchaseOrderStatus.PartialReceive && po.Status != PurchaseOrderStatus.Completed)
            throw new InvalidOperationException(
                $"Purchase Order {po.No} belum ada barang yang diterima (status: {po.Status}). Tidak bisa dibuatkan Supplier Invoice.");

        var invoiceItems = new List<SupplierInvoiceItem>();
        decimal subtotal = 0;

        foreach (var line in request.Items)
        {
            var poItem = po.Items.FirstOrDefault(i => i.Id == line.PurchaseOrderItemId)
                ?? throw new InvalidOperationException($"Item PO dengan Id {line.PurchaseOrderItemId} tidak ditemukan di PO {po.No}.");

            var availableToInvoice = poItem.ReceivedQty - poItem.InvoicedQty;
            if (line.Qty <= 0)
                throw new ArgumentException($"Qty invoice untuk {poItem.ItemName} harus lebih dari 0.");

            if (line.Qty > availableToInvoice)
                throw new InvalidOperationException(
                    $"Qty invoice untuk {poItem.ItemName} ({line.Qty}) melebihi qty yang belum di-invoice ({availableToInvoice} — sudah diterima {poItem.ReceivedQty}, sudah di-invoice {poItem.InvoicedQty}).");

            poItem.InvoicedQty += line.Qty;

            var amount = Math.Round(line.Qty * poItem.Price, 2);
            subtotal += amount;

            invoiceItems.Add(new SupplierInvoiceItem
            {
                PurchaseOrderItemId = poItem.Id,
                Qty = line.Qty,
                Price = poItem.Price,
            });
        }

        subtotal = Math.Round(subtotal, 2);
        var total = subtotal + request.PPNMasukan;

        var no = await NextNumberAsync();

        var invoice = new Models.SupplierInvoice
        {
            No            = no,
            InvoiceNumber = request.InvoiceNumber,
            PurchaseOrderId = po.Id,
            SupplierId    = po.SupplierId,
            InvoiceDate   = request.InvoiceDate,
            DueDate       = request.DueDate,
            Subtotal      = subtotal,
            PPNMasukan    = request.PPNMasukan,
            NomorFakturPajak = request.NomorFakturPajak,
            Total         = total,
            Status        = SupplierInvoiceStatus.Draft,
            Items         = invoiceItems,
        };

        _db.SupplierInvoices.Add(invoice);
        await _db.SaveChangesAsync();

        return (await GetByIdAsync(invoice.Id))!;
    }

    public async Task<SupplierInvoiceDto?> ApproveAsync(Guid id, Guid? userId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var invoice = await _db.SupplierInvoices
            .Include(x => x.PurchaseOrder)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (invoice is null) return null;

        if (invoice.Status != SupplierInvoiceStatus.Draft)
            throw new InvalidOperationException($"Hanya Supplier Invoice berstatus Draft yang bisa di-approve. Status saat ini: {invoice.Status}");

        invoice.Status     = SupplierInvoiceStatus.Approved;
        invoice.ApprovedAt = DateTimeOffset.UtcNow;
        invoice.ApprovedBy = userId;
        invoice.UpdatedAt  = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

        // Reklas GRNI -> Utang Usaha: Debit GRNI (nilai barang) + Debit PPN Masukan, Kredit Utang Usaha (total tagihan).
        await _journalPostingService.PostAsync(
            $"Supplier Invoice {invoice.No} ({invoice.InvoiceNumber})",
            JournalSourceType.PurchaseInvoice,
            invoice.Id,
            DateTimeOffset.UtcNow,
            new PostingLine[]
            {
                new("1-3500", invoice.Subtotal, 0, "Reklas Barang Diterima Belum Ditagih"),
                new("2-3000", invoice.PPNMasukan, 0, "PPN Masukan"),
                new("2-1000", 0, invoice.Total, "Utang Usaha"),
            });

        await tx.CommitAsync();

        return (await GetByIdAsync(id))!;
    }

    public async Task<SupplierInvoiceDto?> RecordPaymentAsync(Guid id, RecordPOPaymentRequest request)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var invoice = await _db.SupplierInvoices
            .Include(x => x.Payments)
                .ThenInclude(p => p.POPayment)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (invoice is null) return null;

        if (invoice.Status != SupplierInvoiceStatus.Approved && invoice.Status != SupplierInvoiceStatus.PartiallyPaid)
            throw new InvalidOperationException(
                $"Hanya Supplier Invoice berstatus Approved atau PartiallyPaid yang bisa dibayar. Status saat ini: {invoice.Status}");

        var alreadyPaid = invoice.Payments.Sum(p => p.POPayment.Amount);
        var newTotalPaid = alreadyPaid + request.Amount;

        if (newTotalPaid > invoice.Total)
            throw new InvalidOperationException(
                $"Total pembayaran (Rp {newTotalPaid:N0}) melebihi total Supplier Invoice (Rp {invoice.Total:N0}).");

        // Bridge approach (Fase 4, disetujui): POPayment tetap dibuat lewat jalur Fase 2 yang sama persis
        // (PurchaseOrderService), TIDAK ada logic/posting duplikat di sini. SupplierInvoicePayment murni
        // menghubungkan POPayment itu ke SupplierInvoice ini untuk keperluan tracking Status/AP Aging.
        var paymentId = await _purchaseOrderService.RecordPaymentForSupplierInvoiceAsync(invoice.PurchaseOrderId, request);

        _db.SupplierInvoicePayments.Add(new SupplierInvoicePayment
        {
            SupplierInvoiceId = invoice.Id,
            POPaymentId       = paymentId,
        });

        invoice.Status    = newTotalPaid >= invoice.Total ? SupplierInvoiceStatus.Paid : SupplierInvoiceStatus.PartiallyPaid;
        invoice.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return (await GetByIdAsync(id))!;
    }

    private async Task<string> NextNumberAsync()
    {
        var config = await _db.NumberingConfigs
            .FirstOrDefaultAsync(n => n.DocType == "SUPPLIER_INVOICE")
            ?? throw new InvalidOperationException("NumberingConfig for SUPPLIER_INVOICE not found");

        var no = config.GenerateNext();
        await _db.SaveChangesAsync();
        return no;
    }

    private static SupplierInvoiceListDto ToListDto(Models.SupplierInvoice x)
    {
        var paid = x.Payments.Sum(p => p.POPayment.Amount);
        return new SupplierInvoiceListDto
        {
            Id              = x.Id,
            No              = x.No,
            InvoiceNumber   = x.InvoiceNumber,
            PurchaseOrderNo = x.PurchaseOrder?.No ?? string.Empty,
            SupplierName    = x.Supplier?.Name ?? string.Empty,
            InvoiceDate     = x.InvoiceDate,
            DueDate         = x.DueDate,
            Total           = x.Total,
            Paid            = paid,
            Balance         = x.Total - paid,
            Status          = x.Status.ToString(),
        };
    }

    private static SupplierInvoiceDto ToDto(Models.SupplierInvoice x)
    {
        var paid = x.Payments.Sum(p => p.POPayment.Amount);
        return new SupplierInvoiceDto
        {
            Id              = x.Id,
            No              = x.No,
            InvoiceNumber   = x.InvoiceNumber,
            PurchaseOrderId = x.PurchaseOrderId,
            PurchaseOrderNo = x.PurchaseOrder?.No ?? string.Empty,
            SupplierId      = x.SupplierId,
            SupplierName    = x.Supplier?.Name ?? string.Empty,
            InvoiceDate     = x.InvoiceDate,
            DueDate         = x.DueDate,
            Subtotal        = x.Subtotal,
            PPNMasukan      = x.PPNMasukan,
            NomorFakturPajak = x.NomorFakturPajak,
            Total           = x.Total,
            Paid            = paid,
            Balance         = x.Total - paid,
            Status          = x.Status.ToString(),
            ApprovedAt      = x.ApprovedAt,
            CreatedAt       = x.CreatedAt,
            Items = x.Items.Select(i => new SupplierInvoiceItemDto
            {
                Id                  = i.Id,
                PurchaseOrderItemId = i.PurchaseOrderItemId,
                ItemName            = i.PurchaseOrderItem?.ItemName ?? string.Empty,
                Qty                 = i.Qty,
                Price               = i.Price,
                Amount              = i.Amount,
            }).ToList(),
        };
    }
}
