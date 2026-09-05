using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.JournalEntry;
using SynteraERP.Api.DTOs.Reports;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class ReportsService : IReportsService
{
    private readonly AppDbContext _db;
    private readonly IJournalPostingService _journalPostingService;

    public ReportsService(AppDbContext db, IJournalPostingService journalPostingService)
    {
        _db = db;
        _journalPostingService = journalPostingService;
    }

    public Task<List<TrialBalanceRowDto>> GetTrialBalanceAsync(DateOnly? asOfDate)
    {
        var cutoff = ToEndOfDay(asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow));
        return _journalPostingService.GetTrialBalanceAsync(cutoff);
    }

    public async Task<IncomeStatementDto> GetIncomeStatementAsync(DateOnly? startDate, DateOnly? endDate)
    {
        var end = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = startDate ?? new DateOnly(end.Year, end.Month, 1);

        var startOffset = ToStartOfDay(start);
        var endOffset = ToEndOfDay(end);

        // Reversed entries tetap diikutkan (bukan cuma Posted) — pola sama seperti GetTrialBalanceAsync
        // di Fase 1: jurnal pembalik menetralkan entri asal, kalau entri asal (Reversed) dikecualikan,
        // reversal-nya jadi berdiri sendiri tanpa pasangan dan Laba Rugi jadi salah hitung.
        var raw = await _db.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Include(l => l.Account)
            .Where(l => l.JournalEntry.Status != JournalEntryStatus.Draft
                     && l.JournalEntry.Date >= startOffset && l.JournalEntry.Date <= endOffset
                     && (l.Account.Type == AccountType.Revenue || l.Account.Type == AccountType.Expense))
            .Select(l => new
            {
                l.AccountId,
                AccountCode = l.Account.Code,
                AccountName = l.Account.Name,
                AccountType = l.Account.Type,
                l.Debit,
                l.Credit,
            })
            .ToListAsync();

        var grouped = raw
            .GroupBy(x => new { x.AccountId, x.AccountCode, x.AccountName, x.AccountType })
            .Select(g => new
            {
                g.Key.AccountId,
                g.Key.AccountCode,
                g.Key.AccountName,
                g.Key.AccountType,
                Amount = g.Key.AccountType == AccountType.Revenue
                    ? g.Sum(x => x.Credit) - g.Sum(x => x.Debit)
                    : g.Sum(x => x.Debit) - g.Sum(x => x.Credit),
            })
            .ToList();

        var revenues = grouped
            .Where(x => x.AccountType == AccountType.Revenue)
            .OrderBy(x => x.AccountCode)
            .Select(x => new IncomeStatementAccountRowDto { AccountId = x.AccountId, AccountCode = x.AccountCode, AccountName = x.AccountName, Amount = x.Amount })
            .ToList();

        var expenses = grouped
            .Where(x => x.AccountType == AccountType.Expense)
            .OrderBy(x => x.AccountCode)
            .Select(x => new IncomeStatementAccountRowDto { AccountId = x.AccountId, AccountCode = x.AccountCode, AccountName = x.AccountName, Amount = x.Amount })
            .ToList();

        var totalRevenue = revenues.Sum(x => x.Amount);
        var totalExpense = expenses.Sum(x => x.Amount);

        return new IncomeStatementDto
        {
            StartDate = start,
            EndDate = end,
            Revenues = revenues,
            Expenses = expenses,
            TotalRevenue = totalRevenue,
            TotalExpense = totalExpense,
            NetIncome = totalRevenue - totalExpense,
        };
    }

    public async Task<BalanceSheetDto> GetBalanceSheetAsync(DateOnly? asOfDate)
    {
        var effectiveDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var cutoff = ToEndOfDay(effectiveDate);

        // Sama seperti GetTrialBalanceAsync (Fase 1): kumulatif sejak awal sampai cutoff, Reversed tetap
        // diikutkan supaya jurnal pembalik menetralkan entri asalnya.
        var raw = await _db.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Include(l => l.Account)
            .Where(l => l.JournalEntry.Status != JournalEntryStatus.Draft && l.JournalEntry.Date <= cutoff)
            .Select(l => new
            {
                l.AccountId,
                AccountCode = l.Account.Code,
                AccountName = l.Account.Name,
                AccountType = l.Account.Type,
                l.Debit,
                l.Credit,
            })
            .ToListAsync();

        var grouped = raw
            .GroupBy(x => new { x.AccountId, x.AccountCode, x.AccountName, x.AccountType })
            .Select(g => new
            {
                g.Key.AccountId,
                g.Key.AccountCode,
                g.Key.AccountName,
                g.Key.AccountType,
                TotalDebit = g.Sum(x => x.Debit),
                TotalCredit = g.Sum(x => x.Credit),
            })
            .ToList();

        var assets = grouped
            .Where(x => x.AccountType == AccountType.Asset)
            .OrderBy(x => x.AccountCode)
            .Select(x => new BalanceSheetAccountRowDto { AccountId = x.AccountId, AccountCode = x.AccountCode, AccountName = x.AccountName, Balance = x.TotalDebit - x.TotalCredit })
            .ToList();

        var liabilities = grouped
            .Where(x => x.AccountType == AccountType.Liability)
            .OrderBy(x => x.AccountCode)
            .Select(x => new BalanceSheetAccountRowDto { AccountId = x.AccountId, AccountCode = x.AccountCode, AccountName = x.AccountName, Balance = x.TotalCredit - x.TotalDebit })
            .ToList();

        var equities = grouped
            .Where(x => x.AccountType == AccountType.Equity)
            .OrderBy(x => x.AccountCode)
            .Select(x => new BalanceSheetAccountRowDto { AccountId = x.AccountId, AccountCode = x.AccountCode, AccountName = x.AccountName, Balance = x.TotalCredit - x.TotalDebit })
            .ToList();

        // Laba Rugi Berjalan (Belum Ditutup): sistem ini belum punya proses Period Closing (item roadmap
        // jangka panjang yang belum dikerjakan) yang memindahkan saldo Revenue/Expense ke Equity. Tanpa
        // baris plug ini, Neraca TIDAK AKAN PERNAH balance (Asset akan selalu lebih besar dari
        // Liability+Equity persis sebesar akumulasi Laba/Rugi sejak awal) — bukan karena bug, tapi karena
        // secara akuntansi laba/rugi yang belum ditutup MEMANG bagian dari Equity. Baris ini dihitung live
        // dari akumulasi Revenue-Expense s.d. cutoff, bukan dari jurnal penutup manual.
        var revenueTotal = grouped.Where(x => x.AccountType == AccountType.Revenue).Sum(x => x.TotalCredit - x.TotalDebit);
        var expenseTotal = grouped.Where(x => x.AccountType == AccountType.Expense).Sum(x => x.TotalDebit - x.TotalCredit);
        var netIncomeToDate = revenueTotal - expenseTotal;

        equities.Add(new BalanceSheetAccountRowDto
        {
            AccountCode = "-",
            AccountName = "Laba Rugi Berjalan (Belum Ditutup)",
            Balance = netIncomeToDate,
        });

        var totalAssets = assets.Sum(x => x.Balance);
        var totalLiabilities = liabilities.Sum(x => x.Balance);
        var totalEquities = equities.Sum(x => x.Balance);

        return new BalanceSheetDto
        {
            AsOfDate = cutoff,
            Assets = assets,
            Liabilities = liabilities,
            Equities = equities,
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities,
            TotalEquities = totalEquities,
            Selisih = totalAssets - (totalLiabilities + totalEquities),
        };
    }

    public async Task<GeneralLedgerDto?> GetGeneralLedgerAsync(Guid accountId, DateOnly? startDate, DateOnly? endDate)
    {
        var account = await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == accountId && !x.IsDeleted);
        if (account is null) return null;

        var end = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = startDate ?? new DateOnly(end.Year, end.Month, 1);

        var startOffset = ToStartOfDay(start);
        var endOffset = ToEndOfDay(end);
        var isDebitNormal = account.NormalBalance == NormalBalanceType.Debit;

        var openingRaw = await _db.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Where(l => l.AccountId == accountId && l.JournalEntry.Status != JournalEntryStatus.Draft && l.JournalEntry.Date < startOffset)
            .Select(l => new { l.Debit, l.Credit })
            .ToListAsync();

        var openingDebit = openingRaw.Sum(x => x.Debit);
        var openingCredit = openingRaw.Sum(x => x.Credit);
        var openingBalance = isDebitNormal ? openingDebit - openingCredit : openingCredit - openingDebit;

        var lines = await _db.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Where(l => l.AccountId == accountId && l.JournalEntry.Status != JournalEntryStatus.Draft
                     && l.JournalEntry.Date >= startOffset && l.JournalEntry.Date <= endOffset)
            .OrderBy(l => l.JournalEntry.Date).ThenBy(l => l.JournalEntry.EntryNumber)
            .Select(l => new
            {
                l.JournalEntry.Date,
                l.JournalEntry.EntryNumber,
                l.JournalEntry.Description,
                l.Debit,
                l.Credit,
                l.Memo,
            })
            .ToListAsync();

        var running = openingBalance;
        var rows = new List<GeneralLedgerLineDto>();
        foreach (var line in lines)
        {
            running += isDebitNormal ? (line.Debit - line.Credit) : (line.Credit - line.Debit);
            rows.Add(new GeneralLedgerLineDto
            {
                Date = line.Date,
                EntryNumber = line.EntryNumber,
                Description = string.IsNullOrWhiteSpace(line.Memo) ? line.Description : line.Memo!,
                Debit = line.Debit,
                Credit = line.Credit,
                RunningBalance = running,
            });
        }

        return new GeneralLedgerDto
        {
            AccountId = account.Id,
            AccountCode = account.Code,
            AccountName = account.Name,
            NormalBalance = account.NormalBalance.ToString(),
            StartDate = start,
            EndDate = end,
            OpeningBalance = openingBalance,
            Lines = rows,
            ClosingBalance = running,
        };
    }

    public async Task<PpnReconciliationDto> GetPpnReconciliationAsync(DateOnly? startDate, DateOnly? endDate)
    {
        var end = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = startDate ?? new DateOnly(end.Year, end.Month, 1);

        var startOffset = ToStartOfDay(start);
        var endOffset = ToEndOfDay(end);

        // Sumber kebenaran adalah JournalEntryLine yang sudah diposting (bukan reverse-calculate dari
        // Invoice.Amount) — supaya angka di laporan ini tidak pernah melenceng dari GL kalau TaxRate
        // default berubah di kemudian hari (lihat catatan investigasi PPN Reconciliation). Reversed
        // entries tetap diikutkan (pola sama seperti GetTrialBalanceAsync) supaya jurnal pembalik
        // menetralkan entri asal alih-alih dobel-hitung.
        var rawKeluaran = await _db.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Include(l => l.Account)
            .Where(l => l.Account.Code == "2-2000"
                     && l.JournalEntry.Status != JournalEntryStatus.Draft
                     && l.JournalEntry.Date >= startOffset && l.JournalEntry.Date <= endOffset)
            .Select(l => new
            {
                l.JournalEntry.Date,
                l.JournalEntry.EntryNumber,
                l.JournalEntry.Description,
                l.JournalEntry.SourceType,
                l.JournalEntry.SourceId,
                l.Debit,
                l.Credit,
            })
            .ToListAsync();

        var rawMasukan = await _db.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Include(l => l.Account)
            .Where(l => l.Account.Code == "2-3000"
                     && l.JournalEntry.Status != JournalEntryStatus.Draft
                     && l.JournalEntry.Date >= startOffset && l.JournalEntry.Date <= endOffset)
            .Select(l => new
            {
                l.JournalEntry.Date,
                l.JournalEntry.EntryNumber,
                l.JournalEntry.Description,
                l.JournalEntry.SourceType,
                l.JournalEntry.SourceId,
                l.Debit,
                l.Credit,
            })
            .ToListAsync();

        // Resolve detail dokumen asal (No Invoice/SupplierInvoice, nama Customer/Supplier, NPWP, Nomor
        // Faktur Pajak) hanya untuk baris yang SourceType-nya langsung merujuk Invoice/SupplierInvoice.
        // Baris lain (mis. Reversal, atau ManualAdjustment/OpeningBalance yang kebetulan menyentuh akun
        // ini) tetap ikut di total, tapi kolom dokumennya fallback ke JournalEntry.Description — tidak
        // dipaksakan resolve 2-hop yang rapuh untuk kasus tepi yang jarang terjadi.
        var invoiceIds = rawKeluaran
            .Where(x => x.SourceType == JournalSourceType.SalesInvoice && x.SourceId.HasValue)
            .Select(x => x.SourceId!.Value).Distinct().ToList();
        var invoices = await _db.Invoices.AsNoTracking().Include(i => i.Customer)
            .Where(i => invoiceIds.Contains(i.Id)).ToListAsync();
        var invoiceById = invoices.ToDictionary(i => i.Id);

        var supplierInvoiceIds = rawMasukan
            .Where(x => x.SourceType == JournalSourceType.PurchaseInvoice && x.SourceId.HasValue)
            .Select(x => x.SourceId!.Value).Distinct().ToList();
        var supplierInvoices = await _db.SupplierInvoices.AsNoTracking().Include(si => si.Supplier)
            .Where(si => supplierInvoiceIds.Contains(si.Id)).ToListAsync();
        var supplierInvoiceById = supplierInvoices.ToDictionary(si => si.Id);

        var ppnKeluaran = rawKeluaran.Select(x =>
        {
            Models.Invoice? inv = x.SourceType == JournalSourceType.SalesInvoice && x.SourceId.HasValue
                ? invoiceById.GetValueOrDefault(x.SourceId.Value) : null;
            return new PpnReconciliationRowDto
            {
                Date             = x.Date,
                EntryNumber      = x.EntryNumber,
                SourceType       = x.SourceType.ToString(),
                DocumentNo       = inv?.No ?? x.Description,
                PartnerName      = inv?.Customer.Name,
                Npwp             = inv?.Customer.Npwp,
                NomorFakturPajak = inv?.NomorFakturPajak,
                Amount           = x.Credit - x.Debit,
            };
        }).OrderBy(x => x.Date).ThenBy(x => x.EntryNumber).ToList();

        var ppnMasukan = rawMasukan.Select(x =>
        {
            Models.SupplierInvoice? si = x.SourceType == JournalSourceType.PurchaseInvoice && x.SourceId.HasValue
                ? supplierInvoiceById.GetValueOrDefault(x.SourceId.Value) : null;
            return new PpnReconciliationRowDto
            {
                Date             = x.Date,
                EntryNumber      = x.EntryNumber,
                SourceType       = x.SourceType.ToString(),
                DocumentNo       = si?.No ?? x.Description,
                PartnerName      = si?.Supplier.Name,
                Npwp             = si?.Supplier.Npwp,
                NomorFakturPajak = si?.NomorFakturPajak,
                Amount           = x.Debit - x.Credit,
            };
        }).OrderBy(x => x.Date).ThenBy(x => x.EntryNumber).ToList();

        var totalKeluaran = ppnKeluaran.Sum(x => x.Amount);
        var totalMasukan = ppnMasukan.Sum(x => x.Amount);

        return new PpnReconciliationDto
        {
            StartDate        = start,
            EndDate          = end,
            PpnKeluaran      = ppnKeluaran,
            PpnMasukan       = ppnMasukan,
            TotalPpnKeluaran = totalKeluaran,
            TotalPpnMasukan  = totalMasukan,
            Selisih          = totalKeluaran - totalMasukan,
        };
    }

    private static DateTimeOffset ToStartOfDay(DateOnly date) =>
        new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    private static DateTimeOffset ToEndOfDay(DateOnly date) =>
        new(date.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
}
