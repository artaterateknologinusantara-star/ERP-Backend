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
    private readonly IJournalPostingService _journalPostingService;

    public PurchaseOrderService(AppDbContext db, IJournalPostingService journalPostingService)
    {
        _db = db;
        _journalPostingService = journalPostingService;
    }

    public async Task<PaginatedResponse<PurchaseOrderListDto>> ListAsync(PaginationParams p, Guid? purchaseRequestId = null, IEnumerable<Guid>? purchaseRequestIds = null)
    {
        var q = _db.PurchaseOrders
            .Include(x => x.Supplier)
            .Include(x => x.PurchaseRequest)
            .AsQueryable();

        if (purchaseRequestId.HasValue)
        {
            q = q.Where(x => x.PurchaseRequestId == purchaseRequestId.Value);
        }

        var idList = purchaseRequestIds?.ToList();
        if (idList is { Count: > 0 })
        {
            q = q.Where(x => x.PurchaseRequestId.HasValue && idList.Contains(x.PurchaseRequestId.Value));
        }

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
            .Include(x => x.Payments.OrderBy(p => p.PaymentDate))
            .FirstOrDefaultAsync(x => x.Id == id);

        return po is null ? null : ToDto(po);
    }

    public async Task<POPaymentResponse> RecordPaymentAsync(Guid poId, RecordPOPaymentRequest request)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        // Fase 4: begitu PO punya Supplier Invoice yang sudah Approved/PartiallyPaid, pembayaran WAJIB
        // lewat endpoint Supplier Invoice - supaya SupplierInvoice.Status (dipakai untuk AP Aging) tidak
        // pernah kelewat/telat update karena ada pembayaran yang tercatat di GL tapi tidak tertaut ke invoice.
        var blockingInvoiceNo = await _db.SupplierInvoices
            .Where(x => x.PurchaseOrderId == poId &&
                (x.Status == SupplierInvoiceStatus.Approved || x.Status == SupplierInvoiceStatus.PartiallyPaid))
            .Select(x => x.No)
            .FirstOrDefaultAsync();

        if (blockingInvoiceNo is not null)
            throw new InvalidOperationException(
                $"PO ini sudah memiliki Supplier Invoice {blockingInvoiceNo}. Gunakan endpoint pembayaran Supplier Invoice untuk mencatat pembayaran, bukan endpoint PO langsung.");

        // Cap terhadap po.Total (belum termasuk PPN) HANYA berlaku di jalur langsung ini - PO tidak
        // pernah punya Supplier Invoice untuk sampai ke titik ini (baru saja divalidasi di atas), jadi
        // tidak ada tagihan ber-PPN yang perlu diakomodasi.
        var po = await _db.PurchaseOrders
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == poId && !x.IsDeleted)
            ?? throw new InvalidOperationException("Purchase Order tidak ditemukan.");

        var currentPaid = po.Payments.Sum(x => x.Amount);
        var newTotal    = currentPaid + request.Amount;

        if (newTotal > po.Total)
            throw new InvalidOperationException(
                $"Total pembayaran (Rp {newTotal:N0}) melebihi total PO (Rp {po.Total:N0}).");

        var payment = await RecordPaymentCoreAsync(poId, request);
        await tx.CommitAsync();

        return ToPaymentResponse(payment);
    }

    public async Task<Guid> RecordPaymentForSupplierInvoiceAsync(Guid poId, RecordPOPaymentRequest request)
    {
        // Sengaja TIDAK BeginTransactionAsync di sini - caller (SupplierInvoiceService) sudah membuka
        // transaction sendiri yang membungkus method ini plus penautan SupplierInvoicePayment + update
        // Status, supaya semuanya rollback bersama kalau salah satu gagal.
        // Sengaja TIDAK validasi terhadap po.Total di sini - itu tidak memperhitungkan PPN. Caller
        // (SupplierInvoiceService) sudah validasi terhadap SupplierInvoice.Total (Subtotal+PPN), yang
        // merupakan batas yang benar untuk jalur pembayaran lewat Supplier Invoice.
        var payment = await RecordPaymentCoreAsync(poId, request);
        return payment.Id;
    }

    private async Task<POPayment> RecordPaymentCoreAsync(Guid poId, RecordPOPaymentRequest request)
    {
        var poNo = await _db.PurchaseOrders
            .Where(x => x.Id == poId && !x.IsDeleted)
            .Select(x => x.No)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Purchase Order tidak ditemukan.");

        var payment = new POPayment
        {
            Id              = Guid.NewGuid(),
            PurchaseOrderId = poId,
            PaymentDate     = request.PaymentDate,
            Amount          = request.Amount,
            Method          = request.Method,
            Reference       = request.Reference,
            Notes           = request.Notes,
        };

        _db.POPayments.Add(payment);
        await _db.SaveChangesAsync();

        // Semua Cash In/Out saat ini diposting ke akun Kas (1-1001) karena Payment.Method/POPayment.Method
        // tidak menyimpan info bank account spesifik. Perlu field bank account terstruktur di masa depan
        // untuk rekonsiliasi kas/bank yang akurat.
        // Catatan tambahan: POPayment.Method adalah string bebas (bukan enum terstruktur seperti
        // Payment.Method di sisi AR) - potential cleanup di masa depan, tidak diperbaiki di Fase 2 ini.
        await _journalPostingService.PostAsync(
            $"Pembayaran PO {poNo}",
            JournalSourceType.CashOut,
            payment.Id,
            new DateTimeOffset(request.PaymentDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
            new PostingLine[]
            {
                new("2-1000", request.Amount, 0, "Pelunasan Utang Usaha"),
                new("1-1001", 0, request.Amount, "Kas keluar untuk pembayaran PO"),
            });

        return payment;
    }

    private static POPaymentResponse ToPaymentResponse(POPayment payment) => new()
    {
        Id          = payment.Id,
        PaymentDate = payment.PaymentDate,
        Amount      = payment.Amount,
        Method      = payment.Method,
        Reference   = payment.Reference,
        Notes       = payment.Notes,
        CreatedAt   = payment.CreatedAt,
    };

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

    public async Task<PurchaseOrderDto?> ReceiveGoodsAsync(Guid id, ReceiveGoodsRequest request, Guid userId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var po = await _db.PurchaseOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (po is null) return null;

        decimal totalStockInValue = 0;

        foreach (var recv in request.Items)
        {
            var item = po.Items.FirstOrDefault(i => i.Id == recv.ItemId);
            if (item is null || recv.ReceivedQty <= 0) continue;

            item.ReceivedQty = Math.Min(item.ReceivedQty + recv.ReceivedQty, item.Qty);

            if (item.ItemMasterId.HasValue)
            {
                var master = await _db.ItemMasters.FindAsync(item.ItemMasterId.Value);
                if (master is not null)
                {
                    var stockBefore = master.Stock;
                    var qtyMasuk = recv.ReceivedQty;
                    var unitCostPembelian = item.Price;

                    // Moving Average: NewAvgCost = ((QtyLama*AvgCostLama) + (QtyMasuk*UnitCost)) / (QtyLama+QtyMasuk)
                    master.CurrentAverageCost = Math.Round(
                        ((stockBefore * master.CurrentAverageCost) + (qtyMasuk * unitCostPembelian)) / (stockBefore + qtyMasuk),
                        2);

                    master.Stock += qtyMasuk;
                    master.LastPurchasePrice = item.Price;

                    if (master.PreferredVendorId == null)
                        master.PreferredVendorId = po.SupplierId;

                    master.UpdatedAt = DateTimeOffset.UtcNow;

                    _db.StockTransactions.Add(new StockTransaction
                    {
                        ItemMasterId    = master.Id,
                        Type            = StockTransactionType.StockIn,
                        Source          = StockTransactionSource.PurchaseOrder,
                        Qty             = qtyMasuk,
                        StockBefore     = stockBefore,
                        StockAfter      = master.Stock,
                        RefNo           = po.No,
                        RefId           = po.Id,
                        Notes           = $"Goods Receipt dari PO {po.No}",
                        CreatedByUserId = userId,
                    });

                    totalStockInValue += Math.Round(qtyMasuk * unitCostPembelian, 2);
                }
            }
            // Item PO tanpa ItemMasterId (nama barang bebas tanpa link ke Item Master) sengaja dilewati
            // dari cost tracking & posting GL - konsisten dengan batasan existing (item begitu juga tidak
            // pernah di-track stoknya). Akibatnya saldo Utang Usaha di GL bisa tidak akurat untuk PO yang
            // mengandung item semacam ini - lihat PROJECT_STATUS.md bagian Known Gaps.
        }

        var allReceived = po.Items.All(i => i.ReceivedQty >= i.Qty);
        var anyReceived = po.Items.Any(i => i.ReceivedQty > 0);

        po.Status = allReceived
            ? PurchaseOrderStatus.Completed
            : anyReceived ? PurchaseOrderStatus.PartialReceive : po.Status;

        po.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        // Posting GL: Debit Persediaan / Kredit GRNI (Barang Diterima Belum Ditagih, 1-3500) sebesar
        // total nilai barang yang diterima (hanya mencakup item yang linked ke Item Master - lihat
        // catatan limitation di atas). SENGAJA bukan langsung ke Utang Usaha (2-1000) - itu direklas
        // dari GRNI ke Utang Usaha saat SupplierInvoice di-approve (Fase 4), supaya tidak dobel-kredit
        // Utang Usaha untuk PO yang barangnya sudah diterima tapi tagihan vendornya belum masuk.
        if (totalStockInValue > 0)
        {
            await _journalPostingService.PostAsync(
                $"Penerimaan Barang PO {po.No}",
                JournalSourceType.StockIn,
                po.Id,
                DateTimeOffset.UtcNow,
                new PostingLine[]
                {
                    new("1-3000", totalStockInValue, 0, "Persediaan"),
                    new("1-3500", 0, totalStockInValue, "Barang Diterima Belum Ditagih (GRNI)"),
                });
        }

        await tx.CommitAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<PurchaseOrderDto?> CreateFromPrAsync(Guid prId, CreatePoFromPrRequest request)
    {
        if (request.Items.Count == 0)
            throw new ArgumentException("Purchase Order harus punya minimal 1 item dari Purchase Request.");

        var duplicateIds = request.Items.GroupBy(i => i.PRItemId).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicateIds.Count > 0)
            throw new ArgumentException("Item PR yang sama tidak boleh direferensikan lebih dari 1 kali dalam satu Purchase Order.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var pr = await _db.PurchaseRequests
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == prId && !x.IsDeleted);

        // PO Split per Vendor (Antrian Jangka Panjang #1): 1 PR boleh menghasilkan lebih dari 1 PO,
        // masing-masing PO tetap 1:1 ke satu Supplier, selama PR-nya masih Approved (belum ada PO sama
        // sekali) atau PartiallyOrdered (sebagian item sudah teralokasi ke PO lain, sisanya belum).
        if (pr is null || (pr.Status != Models.PurchaseRequestStatus.Approved && pr.Status != Models.PurchaseRequestStatus.PartiallyOrdered))
            return null;

        var poItems = new List<Models.PurchaseOrderItem>();

        foreach (var reqItem in request.Items)
        {
            var prItem = pr.Items.FirstOrDefault(i => i.Id == reqItem.PRItemId)
                ?? throw new InvalidOperationException($"Item PR dengan Id {reqItem.PRItemId} tidak ditemukan di PR {pr.No}.");

            if (reqItem.Qty <= 0)
                throw new ArgumentException($"Qty untuk {prItem.ItemName} harus lebih dari 0.");

            var remaining = prItem.Qty - prItem.OrderedQty;
            if (reqItem.Qty > remaining)
                throw new InvalidOperationException(
                    $"Qty untuk {prItem.ItemName} ({reqItem.Qty}) melebihi sisa yang belum teralokasi ke PO manapun ({remaining} — total {prItem.Qty}, sudah teralokasi {prItem.OrderedQty}).");

            prItem.OrderedQty += reqItem.Qty;

            poItems.Add(new Models.PurchaseOrderItem
            {
                ItemName     = prItem.ItemName,
                Qty          = reqItem.Qty,
                Unit         = prItem.Unit,
                Price        = prItem.EstPrice,
                ItemMasterId = prItem.ItemMasterId,
            });
        }

        var no = await NextNumberAsync();
        var po = new Models.PurchaseOrder
        {
            No             = no,
            SupplierId     = request.SupplierId,
            PurchaseRequestId = prId,
            Date           = DateOnly.FromDateTime(DateTime.UtcNow),
            DeliveryDate   = request.DeliveryDate,
            Notes          = request.Notes,
            Status         = Models.PurchaseOrderStatus.Draft,
            Items          = poItems,
        };

        po.Total = po.Items.Sum(i => i.Qty * i.Price);

        // Status PR dihitung otomatis (pola sama seperti PurchaseOrderStatus.PartialReceive di
        // ReceiveGoodsAsync) - BUKAN lewat UpdateStatusAsync manual, supaya tidak ada jalur lain yang
        // bisa membuat status PR "Ordered"/"PartiallyOrdered" jadi tidak sinkron dengan OrderedQty item.
        var allFullyOrdered = pr.Items.All(i => i.OrderedQty >= i.Qty);
        pr.Status    = allFullyOrdered ? Models.PurchaseRequestStatus.Ordered : Models.PurchaseRequestStatus.PartiallyOrdered;
        pr.UpdatedAt = DateTimeOffset.UtcNow;

        _db.PurchaseOrders.Add(po);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return (await GetByIdAsync(po.Id))!;
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

    public async Task<PurchaseOrderStatsDto> GetStatsAsync()
    {
        var all = await _db.PurchaseOrders
            .Where(x => !x.IsDeleted)
            .ToListAsync();

        return new PurchaseOrderStatsDto
        {
            Total          = all.Count,
            Draft          = all.Count(x => x.Status == PurchaseOrderStatus.Draft),
            Ordered        = all.Count(x => x.Status == PurchaseOrderStatus.Ordered),
            PartialReceive = all.Count(x => x.Status == PurchaseOrderStatus.PartialReceive),
            Completed      = all.Count(x => x.Status == PurchaseOrderStatus.Completed),
            TotalValue     = all.Sum(x => x.Total),
        };
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

    private static string PurchaseOrderStatusString(PurchaseOrderStatus s) =>
        s == PurchaseOrderStatus.PartialReceive ? "Partial Receive" : s.ToString();

    private static PurchaseOrderListDto ToListDto(Models.PurchaseOrder x) => new()
    {
        Id                = x.Id,
        No                = x.No,
        Date              = x.Date,
        SupplierName      = x.Supplier?.Name ?? string.Empty,
        PurchaseRequestNo = x.PurchaseRequest?.No,
        PurchaseRequestId = x.PurchaseRequestId,
        Status            = PurchaseOrderStatusString(x.Status),
        Total             = x.Total,
        DeliveryDate      = x.DeliveryDate,
    };

    private static PurchaseOrderDto ToDto(Models.PurchaseOrder x)
    {
        var totalPaid = x.Payments.Sum(p => p.Amount);
        return new PurchaseOrderDto
        {
            Id                = x.Id,
            No                = x.No,
            Date              = x.Date,
            SupplierName      = x.Supplier?.Name ?? string.Empty,
            PurchaseRequestNo = x.PurchaseRequest?.No,
            Status            = PurchaseOrderStatusString(x.Status),
            Total             = x.Total,
            DeliveryDate      = x.DeliveryDate,
            SupplierId        = x.SupplierId,
            PurchaseRequestId = x.PurchaseRequestId,
            Notes             = x.Notes,
            CreatedAt         = x.CreatedAt,
            TotalPaid         = totalPaid,
            Balance           = x.Total - totalPaid,
            Items = x.Items.Select(i => new PurchaseOrderItemDto
            {
                Id           = i.Id,
                ItemName     = i.ItemName,
                Qty          = i.Qty,
                Unit         = i.Unit,
                Price        = i.Price,
                Total        = i.Total,
                ReceivedQty  = i.ReceivedQty,
                ItemMasterId = i.ItemMasterId,
            }).ToList(),
            Payments = x.Payments.OrderBy(p => p.PaymentDate).Select(p => new POPaymentResponse
            {
                Id          = p.Id,
                PaymentDate = p.PaymentDate,
                Amount      = p.Amount,
                Method      = p.Method,
                Reference   = p.Reference,
                Notes       = p.Notes,
                CreatedAt   = p.CreatedAt,
            }).ToList(),
        };
    }
}
