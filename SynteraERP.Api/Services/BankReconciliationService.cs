using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.BankReconciliation;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class BankReconciliationService : IBankReconciliationService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    private static readonly string[] BankAccountCodes = ["1-1001", "1-1002", "1-1003", "1-1004"];

    public BankReconciliationService(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<BankStatementImportResult> ImportAsync(ImportBankStatementRequest request, IFormFile file)
    {
        if (file is not { Length: > 0 })
            throw new ArgumentException("File CSV wajib diupload.");

        var ext = Path.GetExtension(file.FileName);
        if (!string.Equals(ext, ".csv", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("File harus berformat .csv.");

        var account = await _db.Accounts.FirstOrDefaultAsync(x => x.Id == request.AccountId && !x.IsDeleted)
            ?? throw new KeyNotFoundException("Akun tidak ditemukan.");

        List<ParsedBankStatementRow> rows;
        List<CsvRowError> parseErrors;
        await using (var stream = file.OpenReadStream())
        {
            (rows, parseErrors) = BankStatementCsvParser.Parse(stream);
        }

        if (parseErrors.Count > 0)
            return new BankStatementImportResult { Success = false, RowErrors = parseErrors };

        // Simpan file ke disk HANYA setelah lolos validasi penuh - tidak ada import setengah
        // jalan kalau ada baris error (lihat §3 spec: reject seluruh file, jangan partial-import).
        var uploadsDir = Path.Combine(_env.ContentRootPath, "uploads", "bank-statements");
        Directory.CreateDirectory(uploadsDir);
        var storedName = $"{Guid.NewGuid()}.csv";
        var fullPath = Path.Combine(uploadsDir, storedName);
        await using (var destStream = new FileStream(fullPath, FileMode.Create))
        await using (var srcStream = file.OpenReadStream())
        {
            await srcStream.CopyToAsync(destStream);
        }

        await using var tx = await _db.Database.BeginTransactionAsync();

        var import = new BankStatementImport
        {
            AccountId = request.AccountId,
            ImportDate = DateTimeOffset.UtcNow,
            FileName = file.FileName,
            FilePath = Path.Combine("bank-statements", storedName),
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            StatementEndingBalance = request.StatementEndingBalance,
        };
        _db.BankStatementImports.Add(import);

        foreach (var row in rows)
        {
            _db.BankStatementLines.Add(new BankStatementLine
            {
                BankStatementImportId = import.Id,
                AccountId = request.AccountId,
                TransactionDate = row.TransactionDate,
                Description = row.Description,
                Amount = row.Amount,
                MatchStatus = BankStatementLineMatchStatus.Unmatched,
            });
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return new BankStatementImportResult
        {
            Success = true,
            Summary = new BankStatementImportSummaryDto { Id = import.Id, LineCount = rows.Count },
        };
    }

    public async Task<List<BankStatementImportListDto>> ListImportsAsync(Guid accountId)
    {
        var imports = await _db.BankStatementImports
            .AsNoTracking()
            .Include(x => x.Lines)
            .Where(x => x.AccountId == accountId)
            .OrderByDescending(x => x.ImportDate)
            .ToListAsync();

        return imports.Select(x => new BankStatementImportListDto
        {
            Id = x.Id,
            ImportDate = x.ImportDate,
            FileName = x.FileName,
            PeriodStart = x.PeriodStart,
            PeriodEnd = x.PeriodEnd,
            StatementEndingBalance = x.StatementEndingBalance,
            LineCount = x.Lines.Count,
            MatchedCount = x.Lines.Count(l => l.MatchStatus == BankStatementLineMatchStatus.Matched),
            UnmatchedCount = x.Lines.Count(l => l.MatchStatus == BankStatementLineMatchStatus.Unmatched),
            IgnoredCount = x.Lines.Count(l => l.MatchStatus == BankStatementLineMatchStatus.Ignored),
        }).ToList();
    }

    public async Task<BankStatementImportDetailDto?> GetImportDetailAsync(Guid id)
    {
        var import = await _db.BankStatementImports
            .AsNoTracking()
            .Include(x => x.Account)
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (import is null) return null;

        var unmatchedLines = import.Lines.Where(l => l.MatchStatus == BankStatementLineMatchStatus.Unmatched).ToList();
        var candidatesByLineId = await BuildMatchCandidatesAsync(import.AccountId, unmatchedLines);

        return new BankStatementImportDetailDto
        {
            Id = import.Id,
            AccountId = import.AccountId,
            AccountCode = import.Account.Code,
            AccountName = import.Account.Name,
            ImportDate = import.ImportDate,
            FileName = import.FileName,
            PeriodStart = import.PeriodStart,
            PeriodEnd = import.PeriodEnd,
            StatementEndingBalance = import.StatementEndingBalance,
            Lines = import.Lines
                .OrderBy(l => l.TransactionDate)
                .Select(l => new BankStatementLineDetailDto
                {
                    Id = l.Id,
                    TransactionDate = l.TransactionDate,
                    Description = l.Description,
                    ReferenceNumber = l.ReferenceNumber,
                    Amount = l.Amount,
                    MatchStatus = l.MatchStatus.ToString(),
                    MatchedJournalEntryLineId = l.MatchedJournalEntryLineId,
                    SuggestedMatches = candidatesByLineId.TryGetValue(l.Id, out var c) ? c : [],
                })
                .ToList(),
        };
    }

    // Query kandidat 1x untuk seluruh rentang tanggal ±3 hari dari semua baris Unmatched di
    // import ini (bukan query per baris) supaya tidak N+1 - lalu difilter per baris di memory.
    private async Task<Dictionary<Guid, List<MatchCandidateDto>>> BuildMatchCandidatesAsync(
        Guid accountId, List<BankStatementLine> unmatchedLines)
    {
        var result = new Dictionary<Guid, List<MatchCandidateDto>>();
        if (unmatchedLines.Count == 0) return result;

        var minDate = unmatchedLines.Min(l => l.TransactionDate).AddDays(-3);
        var maxDate = unmatchedLines.Max(l => l.TransactionDate).AddDays(3);
        var rangeStart = new DateTimeOffset(minDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var rangeEnd = new DateTimeOffset(maxDate.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        var alreadyMatchedIds = await _db.BankStatementLines
            .Where(x => x.MatchedJournalEntryLineId != null)
            .Select(x => x.MatchedJournalEntryLineId!.Value)
            .ToListAsync();

        var candidates = await _db.JournalEntryLines
            .AsNoTracking()
            .Include(l => l.JournalEntry)
            .Where(l => l.AccountId == accountId
                     && l.JournalEntry.Status == JournalEntryStatus.Posted
                     && l.JournalEntry.Date >= rangeStart && l.JournalEntry.Date <= rangeEnd
                     && !alreadyMatchedIds.Contains(l.Id))
            .Select(l => new
            {
                l.Id,
                JournalEntryId = l.JournalEntry.Id,
                l.JournalEntry.EntryNumber,
                l.JournalEntry.Date,
                l.JournalEntry.Description,
                l.Debit,
                l.Credit,
            })
            .ToListAsync();

        foreach (var line in unmatchedLines)
        {
            var windowStart = new DateTimeOffset(line.TransactionDate.AddDays(-3).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var windowEnd = new DateTimeOffset(line.TransactionDate.AddDays(3).ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

            var matches = candidates
                .Where(c => c.Date >= windowStart && c.Date <= windowEnd
                         && (c.Debit - c.Credit) == line.Amount)
                .Select(c => new MatchCandidateDto
                {
                    JournalEntryLineId = c.Id,
                    JournalEntryId = c.JournalEntryId,
                    EntryNumber = c.EntryNumber,
                    Date = c.Date,
                    Description = c.Description,
                    Debit = c.Debit,
                    Credit = c.Credit,
                })
                .ToList();

            result[line.Id] = matches;
        }

        return result;
    }

    public async Task<BankStatementLineDetailDto> MatchAsync(Guid lineId, Guid journalEntryLineId)
    {
        var line = await _db.BankStatementLines.FirstOrDefaultAsync(x => x.Id == lineId)
            ?? throw new KeyNotFoundException("Baris statement bank tidak ditemukan.");

        var jel = await _db.JournalEntryLines
            .Include(x => x.JournalEntry)
            .FirstOrDefaultAsync(x => x.Id == journalEntryLineId)
            ?? throw new KeyNotFoundException("Journal Entry Line tidak ditemukan.");

        if (jel.AccountId != line.AccountId)
            throw new InvalidOperationException("Journal Entry Line yang dipilih bukan untuk akun yang sama dengan baris statement bank ini.");

        if (jel.JournalEntry.Status != JournalEntryStatus.Posted)
            throw new InvalidOperationException("Journal Entry Line yang dipilih harus berstatus Posted (bukan Draft atau Reversed).");

        var alreadyUsed = await _db.BankStatementLines
            .AnyAsync(x => x.MatchedJournalEntryLineId == journalEntryLineId && x.Id != lineId);
        if (alreadyUsed)
            throw new InvalidOperationException("Journal Entry Line ini sudah dipakai untuk match baris statement bank yang lain.");

        line.MatchStatus = BankStatementLineMatchStatus.Matched;
        line.MatchedJournalEntryLineId = journalEntryLineId;
        line.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        return ToLineDetailDto(line);
    }

    public async Task<BankStatementLineDetailDto> UnmatchAsync(Guid lineId)
    {
        var line = await _db.BankStatementLines.FirstOrDefaultAsync(x => x.Id == lineId)
            ?? throw new KeyNotFoundException("Baris statement bank tidak ditemukan.");

        line.MatchStatus = BankStatementLineMatchStatus.Unmatched;
        line.MatchedJournalEntryLineId = null;
        line.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        return ToLineDetailDto(line);
    }

    public async Task<BankStatementLineDetailDto> IgnoreAsync(Guid lineId)
    {
        var line = await _db.BankStatementLines.FirstOrDefaultAsync(x => x.Id == lineId)
            ?? throw new KeyNotFoundException("Baris statement bank tidak ditemukan.");

        line.MatchStatus = BankStatementLineMatchStatus.Ignored;
        line.MatchedJournalEntryLineId = null;
        line.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        return ToLineDetailDto(line);
    }

    private static BankStatementLineDetailDto ToLineDetailDto(BankStatementLine l) => new()
    {
        Id = l.Id,
        TransactionDate = l.TransactionDate,
        Description = l.Description,
        ReferenceNumber = l.ReferenceNumber,
        Amount = l.Amount,
        MatchStatus = l.MatchStatus.ToString(),
        MatchedJournalEntryLineId = l.MatchedJournalEntryLineId,
        SuggestedMatches = [],
    };

    // Endpoint ringan (agregat SUM langsung) - sengaja TIDAK reuse GetGeneralLedgerAsync/
    // GetTrialBalanceAsync karena keduanya fetch seluruh baris mutasi, sementara di sini cuma
    // butuh angka saldo akhir per akun.
    //
    // Catatan penyimpangan dari draft awal: filter status pakai (!= Draft), BUKAN (== Posted)
    // saja. Alasan: entri yang sudah Reversed tetap harus ikut dihitung di saldo - itu pola yang
    // sudah dipakai GetTrialBalanceAsync (lihat komentarnya) karena jurnal pembalik adalah entri
    // TAMBAHAN yang menetralkan entri asal, bukan pengganti. Kalau entri asal (Reversed)
    // dikecualikan padahal jurnal pembaliknya (Posted) tetap dihitung, saldo jadi cuma
    // menghitung sebelah jurnal pembalik saja dan tidak balance ke nol.
    public async Task<List<AccountBalanceDto>> GetBalancesAsync(DateOnly asOf)
    {
        var accounts = await _db.Accounts
            .AsNoTracking()
            .Where(a => BankAccountCodes.Contains(a.Code) && !a.IsDeleted)
            .ToListAsync();

        var accountIds = accounts.Select(a => a.Id).ToList();
        var cutoff = new DateTimeOffset(asOf.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        var sums = await _db.JournalEntryLines
            .AsNoTracking()
            .Where(l => accountIds.Contains(l.AccountId)
                     && l.JournalEntry.Status != JournalEntryStatus.Draft
                     && l.JournalEntry.Date <= cutoff)
            .GroupBy(l => l.AccountId)
            .Select(g => new { AccountId = g.Key, Balance = g.Sum(x => x.Debit) - g.Sum(x => x.Credit) })
            .ToListAsync();

        var sumByAccount = sums.ToDictionary(x => x.AccountId, x => x.Balance);

        return accounts
            .Select(a => new AccountBalanceDto
            {
                AccountId = a.Id,
                AccountCode = a.Code,
                AccountName = a.Name,
                Balance = sumByAccount.TryGetValue(a.Id, out var bal) ? bal : 0m,
            })
            .OrderBy(x => x.AccountCode)
            .ToList();
    }
}
