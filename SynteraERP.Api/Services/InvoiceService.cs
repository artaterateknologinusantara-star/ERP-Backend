using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Invoice;
using SynteraERP.Api.Helpers;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _db;
    private readonly ITaxRateService _taxRateService;
    private readonly IJournalPostingService _journalPostingService;

    public InvoiceService(AppDbContext db, ITaxRateService taxRateService, IJournalPostingService journalPostingService)
    {
        _db = db;
        _taxRateService = taxRateService;
        _journalPostingService = journalPostingService;
    }

    public async Task<PaginatedResponse<InvoiceListDto>> ListAsync(InvoiceQueryParams p)
    {
        // Status Overdue di-refresh oleh InvoiceOverdueStatusService (background job),
        // bukan di sini, supaya endpoint GET ini murni read-only.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var q = _db.Invoices
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.SalesOrder)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(p.Search))
        {
            var s = p.Search.ToLower();
            q = q.Where(x => x.No.ToLower().Contains(s) || x.Customer.Name.ToLower().Contains(s));
        }

        // "Partial Paid" (the display string ToListDto/ToDto render) has a space InvoiceStatus's enum
        // name doesn't, so strip it before parsing -- same trick PurchaseOrderService's "Partial
        // Receive" needs below.
        if (!string.IsNullOrWhiteSpace(p.Status) &&
            Enum.TryParse<InvoiceStatus>(p.Status.Replace(" ", ""), true, out var statusFilter))
        {
            q = q.Where(x => x.Status == statusFilter);
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

        var mapped = data.Select(x => ToListDto(x, today)).ToList();

        return PaginatedResponse<InvoiceListDto>.Create(mapped, total, p.Page, p.PerPage);
    }

    public async Task<InvoiceDto?> GetByIdAsync(Guid id)
    {
        var inv = await _db.Invoices
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.SalesOrder)
            .Include(x => x.Payments)
            .Include(x => x.Items.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(x => x.Id == id);

        if (inv is null) return null;

        var taxRate = await _taxRateService.GetDefaultRateAsync();
        return ToDto(inv, taxRate);
    }

    public async Task<InvoiceDto> CreateAsync(CreateInvoiceRequest request)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var taxRate = await _taxRateService.GetDefaultRateAsync();
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
            NomorFakturPajak = request.NomorFakturPajak,
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
                var subTotalForItems  = MoneyMath.Round(inv.Items.Sum(x => x.Amount));
                var taxAmountForItems = MoneyMath.Round(subTotalForItems * taxRate);
                inv.Amount             = subTotalForItems + taxAmountForItems;
            }
        }

        // Posting GL: Debit Piutang Usaha = Total, Kredit Pendapatan Penjualan = Subtotal, Kredit
        // Utang Pajak Keluaran = PPN. Subtotal/PPN dihitung persis sama seperti ToDto (reverse-calculate
        // dari Amount kalau tidak ada item SO) supaya angka jurnal selalu cocok dengan yang ditampilkan ke user.
        var hasItems = inv.Items != null && inv.Items.Any();
        var subTotal = hasItems
            ? MoneyMath.Round(inv.Items!.Sum(i => i.Amount))
            : MoneyMath.Round(inv.Amount / (1 + taxRate));
        var taxAmount = hasItems
            ? MoneyMath.Round(subTotal * taxRate)
            : inv.Amount - subTotal;

        // Retensi: kalau SalesOrder terkait punya RetentionPercentage > 0, sebagian Piutang Usaha
        // dipindah ke Piutang Retensi (1-2100) — dihitung dari subTotal PRE-TAX, PPN tetap tertagih
        // penuh dan tidak ikut ditahan. Invoice tanpa SO atau RetentionPercentage = 0 tetap
        // RetentionAmount = 0, sama persis dengan perilaku sebelum perubahan ini.
        if (request.SalesOrderId.HasValue)
        {
            var salesOrder = await _db.SalesOrders.FindAsync(request.SalesOrderId.Value);
            if (salesOrder is not null && salesOrder.RetentionPercentage > 0)
                inv.RetentionAmount = MoneyMath.Round(subTotal * (salesOrder.RetentionPercentage / 100m));
        }

        _db.Invoices.Add(inv);
        await _db.SaveChangesAsync();

        // Project POC (Fase B2): kalau SO invoice ini terhubung ke Project ber-status aktif
        // (bukan Cancelled) dengan RevenueRecognitionMethod=PercentageOfCompletion, sisi Kredit
        // invoice TIDAK boleh mengakui Pendapatan Penjualan lagi - itu sudah diakui lebih dulu
        // lewat endpoint Catat Progres (Fase B1). Kredit di sini murni reklas dari "Piutang Belum
        // Ditagih" (1-2200, kalau progres sudah tercatat lebih dulu) dan/atau "Termin Diterima
        // Dimuka" (2-4100, kalau invoice mendahului progres yang tercatat - overbilling). Project
        // null atau method Immediate (mayoritas transaksi) HARUS tetap 100% jalur lama.
        Project? project = inv.SalesOrderId.HasValue
            ? await _db.Projects.FirstOrDefaultAsync(p =>
                p.SalesOrderId == inv.SalesOrderId.Value && p.Status != ProjectStatus.Cancelled)
            : null;

        var lines = new List<PostingLine>();

        // Sisi Debit - tidak berubah dari Fase A.
        if (inv.RetentionAmount > 0)
        {
            lines.Add(new("1-2000", inv.Amount - inv.RetentionAmount, 0, "Piutang Usaha"));
            lines.Add(new("1-2100", inv.RetentionAmount, 0, "Piutang Retensi"));
        }
        else
        {
            lines.Add(new("1-2000", inv.Amount, 0, "Piutang Usaha"));
        }

        // Sisi Kredit - bercabang untuk Project POC.
        var isPoc = project != null && project.RevenueRecognitionMethod == RevenueRecognitionMethod.PercentageOfCompletion;
        if (isPoc)
        {
            var fromUnbilled = Math.Min(subTotal, project!.UnbilledRevenueBalance);
            var overbillPortion = subTotal - fromUnbilled;

            if (fromUnbilled > 0)
                lines.Add(new("1-2200", 0, fromUnbilled, "Piutang Belum Ditagih"));
            if (overbillPortion > 0)
                lines.Add(new("2-4100", 0, overbillPortion, "Termin Diterima Dimuka"));

            project.UnbilledRevenueBalance -= fromUnbilled;
            project.OverbilledBalance += overbillPortion;
            // Sengaja TIDAK ada baris Kredit ke 4-1000 Pendapatan Penjualan di cabang ini -
            // pendapatan sudah diakui lewat Catat Progres. Menambahkannya di sini akan
            // menghitung pendapatan dua kali untuk SO yang sama.
        }
        else
        {
            lines.Add(new("4-1000", 0, subTotal, "Pendapatan Penjualan"));
        }

        lines.Add(new("2-2000", 0, taxAmount, "Utang Pajak Keluaran (PPN Keluaran)"));

        if (isPoc)
            await _db.SaveChangesAsync();

        await _journalPostingService.PostAsync(
            $"Invoice AR {inv.No}",
            JournalSourceType.SalesInvoice,
            inv.Id,
            DateTimeOffset.UtcNow,
            lines);

        await tx.CommitAsync();
        return (await GetByIdAsync(inv.Id))!;
    }

    public async Task<InvoiceDto?> RecordPaymentAsync(Guid id, RecordPaymentRequest request)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var inv = await _db.Invoices
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (inv is null) return null;

        // Cap: tidak boleh menagih lebih dari (Amount - retensi yang belum dilepas). Formula ini
        // otomatis jadi cap terhadap Amount biasa untuk invoice tanpa retensi (RetentionAmount = 0),
        // sekaligus menutup celah overpayment pra-existing yang tidak pernah divalidasi di sini
        // (lihat SalesOrderPaymentService.ApplyToInvoiceAsync untuk pola pesan error yang sama).
        var maxCollectible = inv.Amount - inv.RetentionAmount + inv.RetentionReleasedAmount;
        if (inv.Paid + request.Amount > maxCollectible)
            throw new InvalidOperationException(
                $"Jumlah pembayaran (Rp {request.Amount:N0}) melebihi sisa yang bisa ditagih " +
                $"(Rp {(maxCollectible - inv.Paid):N0}). " +
                $"Retensi sebesar Rp {(inv.RetentionAmount - inv.RetentionReleasedAmount):N0} belum dilepas.");

        if (!Enum.TryParse<PaymentMethod>(request.Method, true, out var method))
            method = PaymentMethod.Transfer;

        var (cashBankAccountId, cashBankAccountCode) = await ResolveCashBankAccountAsync(request.CashBankAccountId);

        var payment = new Payment
        {
            InvoiceId = id,
            PaymentDate = request.Date,
            Amount = request.Amount,
            Method = method,
            CashBankAccountId = cashBankAccountId,
            Reference = request.Reference,
            Notes = request.Notes,
        };

        _db.Payments.Add(payment);
        inv.ApplyPayment(request.Amount);
        inv.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

        await _journalPostingService.PostAsync(
            $"Pembayaran Invoice {inv.No}",
            JournalSourceType.CashIn,
            payment.Id,
            new DateTimeOffset(request.Date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
            new PostingLine[]
            {
                new(cashBankAccountCode, request.Amount, 0, "Kas masuk dari pelunasan piutang"),
                new("1-2000", 0, request.Amount, "Pelunasan Piutang Usaha"),
            });

        await tx.CommitAsync();
        return (await GetByIdAsync(id))!;
    }

    // Pola sama persis dengan ExpenseService.CreateAsync: kalau CashBankAccountId tidak diisi,
    // default ke akun Kas (1-1001) supaya Cash In/Out lama yang tidak mengisi akun tetap
    // ter-posting persis seperti sebelum field ini ada.
    private async Task<(Guid Id, string Code)> ResolveCashBankAccountAsync(Guid? cashBankAccountId)
    {
        if (cashBankAccountId.HasValue)
        {
            var account = await _db.Accounts
                .Where(x => x.Id == cashBankAccountId.Value && !x.IsDeleted)
                .Select(x => new { x.Id, x.Code })
                .FirstOrDefaultAsync();

            if (account is null)
                throw new InvalidOperationException("Akun Kas/Bank yang dipilih tidak ditemukan.");

            return (account.Id, account.Code);
        }

        var defaultAccount = await _db.Accounts
            .Where(x => x.Code == "1-1001" && !x.IsDeleted)
            .Select(x => new { x.Id, x.Code })
            .FirstOrDefaultAsync();

        if (defaultAccount is null)
            throw new InvalidOperationException("Akun default Kas (1-1001) tidak ditemukan di Chart of Accounts.");

        return (defaultAccount.Id, defaultAccount.Code);
    }

    public async Task<InvoiceDto?> ReleaseRetentionAsync(Guid id, RetentionReleaseRequest request)
    {
        if (request.Amount <= 0)
            throw new InvalidOperationException("Jumlah retensi yang dilepas harus lebih besar dari 0.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var inv = await _db.Invoices.FirstOrDefaultAsync(x => x.Id == id);
        if (inv is null) return null;

        var remainingRetention = inv.RetentionAmount - inv.RetentionReleasedAmount;
        if (request.Amount > remainingRetention)
            throw new InvalidOperationException(
                $"Jumlah yang dilepas (Rp {request.Amount:N0}) melebihi sisa retensi yang bisa dilepas " +
                $"(Rp {remainingRetention:N0}).");

        var release = new RetentionRelease
        {
            InvoiceId = id,
            ReleaseDate = request.ReleaseDate,
            Amount = request.Amount,
            Notes = request.Notes,
        };

        _db.RetentionReleases.Add(release);
        inv.RetentionReleasedAmount += request.Amount;
        inv.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

        await _journalPostingService.PostAsync(
            $"Pelepasan Retensi Invoice {inv.No}",
            JournalSourceType.RetentionRelease,
            release.Id,
            request.ReleaseDate,
            new PostingLine[]
            {
                new("1-2000", request.Amount, 0, "Piutang Usaha"),
                new("1-2100", 0, request.Amount, "Piutang Retensi"),
            });

        await tx.CommitAsync();
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
        RetentionAmount = x.RetentionAmount,
        RetentionReleasedAmount = x.RetentionReleasedAmount,
        AgingDays = (x.Status == InvoiceStatus.Overdue && x.DueDate < today)
            ? (today.DayNumber - x.DueDate.DayNumber)
            : 0,
    };

    private static InvoiceDto ToDto(Models.Invoice x, decimal taxRate)
    {
        // Compute subTotal / taxAmount dari items jika ada, otherwise reverse dari Amount
        var hasItems  = x.Items != null && x.Items.Any();
        var subTotal  = hasItems
            ? MoneyMath.Round(x.Items!.Sum(i => i.Amount))
            : MoneyMath.Round(x.Amount / (1 + taxRate));
        var taxAmount = hasItems
            ? MoneyMath.Round(subTotal * taxRate)
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
            RetentionAmount = x.RetentionAmount,
            RetentionReleasedAmount = x.RetentionReleasedAmount,
            CustomerId   = x.CustomerId,
            SalesOrderId = x.SalesOrderId,
            Notes        = x.Notes,
            Terms        = x.Terms,
            NomorFakturPajak = x.NomorFakturPajak,
            SubTotal     = subTotal,
            TaxAmount    = taxAmount,
            CreatedAt    = x.CreatedAt,
            Payments     = x.Payments.OrderBy(p => p.PaymentDate).Select(p => new PaymentDto
            {
                Id          = p.Id,
                PaymentDate = p.PaymentDate,
                Amount      = p.Amount,
                Method      = p.Method.ToString(),
                CashBankAccountId = p.CashBankAccountId,
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
