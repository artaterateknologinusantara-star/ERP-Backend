using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.JournalEntry;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class JournalPostingService : IJournalPostingService
{
    private readonly AppDbContext _db;

    public JournalPostingService(AppDbContext db) => _db = db;

    public async Task<PaginatedResponse<JournalEntryListDto>> ListAsync(JournalEntryQueryParams p)
    {
        var q = _db.JournalEntries.Include(x => x.Lines).AsQueryable();

        if (p.DateFrom.HasValue)
        {
            var from = new DateTimeOffset(p.DateFrom.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            q = q.Where(x => x.Date >= from);
        }

        if (p.DateTo.HasValue)
        {
            var to = new DateTimeOffset(p.DateTo.Value.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
            q = q.Where(x => x.Date <= to);
        }

        if (!string.IsNullOrWhiteSpace(p.Status) && Enum.TryParse<JournalEntryStatus>(p.Status, true, out var status))
            q = q.Where(x => x.Status == status);

        if (!string.IsNullOrWhiteSpace(p.SourceType) && Enum.TryParse<JournalSourceType>(p.SourceType, true, out var sourceType))
            q = q.Where(x => x.SourceType == sourceType);

        if (p.SourceId.HasValue)
            q = q.Where(x => x.SourceId == p.SourceId.Value);

        q = q.OrderByDescending(x => x.Date).ThenByDescending(x => x.EntryNumber);

        var total = await q.CountAsync();
        var data = await q.Skip(p.Skip).Take(p.PerPage)
            .Select(x => new JournalEntryListDto
            {
                Id = x.Id,
                EntryNumber = x.EntryNumber,
                Date = x.Date,
                Description = x.Description,
                SourceType = x.SourceType.ToString(),
                Status = x.Status.ToString(),
                TotalDebit = x.Lines.Sum(l => l.Debit),
                TotalCredit = x.Lines.Sum(l => l.Credit),
            })
            .ToListAsync();

        return PaginatedResponse<JournalEntryListDto>.Create(data, total, p.Page, p.PerPage);
    }

    public async Task<JournalEntryDto?> GetByIdAsync(Guid id)
    {
        var entry = await _db.JournalEntries
            .Include(x => x.Lines)
                .ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(x => x.Id == id);

        return entry is null ? null : ToDto(entry);
    }

    public async Task<JournalEntryDto> CreateManualEntryAsync(CreateJournalEntryRequest req)
    {
        if (req.Lines.Count == 0)
            throw new ArgumentException("Journal entry harus punya minimal 1 baris.");

        if (!Enum.TryParse<JournalSourceType>(req.SourceType, true, out var sourceType))
            throw new ArgumentException($"SourceType '{req.SourceType}' tidak valid.");

        var totalDebit  = Math.Round(req.Lines.Sum(l => l.Debit), 2);
        var totalCredit = Math.Round(req.Lines.Sum(l => l.Credit), 2);

        if (totalDebit != totalCredit)
            throw new InvalidOperationException($"Journal entry tidak balance: Debit {totalDebit}, Credit {totalCredit}");

        var entryNumber = await NextEntryNumberAsync();
        var postNow = req.PostImmediately;

        var entry = new JournalEntry
        {
            EntryNumber = entryNumber,
            Date        = req.Date ?? DateTimeOffset.UtcNow,
            Description = req.Description,
            SourceType  = sourceType,
            SourceId    = req.SourceId,
            Status      = postNow ? JournalEntryStatus.Posted : JournalEntryStatus.Draft,
            PostedAt    = postNow ? DateTimeOffset.UtcNow : null,
            Lines = req.Lines.Select(l => new JournalEntryLine
            {
                AccountId = l.AccountId,
                Debit     = l.Debit,
                Credit    = l.Credit,
                Memo      = l.Memo,
            }).ToList(),
        };

        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync();

        return (await GetByIdAsync(entry.Id))!;
    }

    public async Task<JournalEntryDto> ReverseAsync(Guid journalEntryId)
    {
        var original = await _db.JournalEntries
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == journalEntryId)
            ?? throw new KeyNotFoundException("Journal entry tidak ditemukan.");

        if (original.Status != JournalEntryStatus.Posted)
            throw new InvalidOperationException("Hanya journal entry berstatus Posted yang bisa di-reverse.");

        var entryNumber = await NextEntryNumberAsync();

        var reversal = new JournalEntry
        {
            EntryNumber = entryNumber,
            Date        = DateTimeOffset.UtcNow,
            Description = $"Reversal dari {original.EntryNumber}",
            SourceType  = JournalSourceType.Reversal,
            SourceId    = original.Id,
            Status      = JournalEntryStatus.Posted,
            PostedAt    = DateTimeOffset.UtcNow,
            Lines = original.Lines.Select(l => new JournalEntryLine
            {
                AccountId = l.AccountId,
                Debit     = l.Credit,
                Credit    = l.Debit,
                Memo      = l.Memo,
            }).ToList(),
        };

        _db.JournalEntries.Add(reversal);
        await _db.SaveChangesAsync();

        original.Status           = JournalEntryStatus.Reversed;
        original.ReversedByEntryId = reversal.Id;
        original.UpdatedAt        = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        return (await GetByIdAsync(reversal.Id))!;
    }

    public async Task<List<TrialBalanceRowDto>> GetTrialBalanceAsync(DateTimeOffset? asOfDate)
    {
        var cutoff = asOfDate ?? DateTimeOffset.UtcNow;

        // Entri Reversed tetap dihitung (bukan cuma Posted): jurnal pembalik adalah transaksi
        // TAMBAHAN yang menetralkan entri asal, bukan pengganti — kalau entri asal (Reversed)
        // dikecualikan, efek pembalikannya jadi dobel-hitung dan saldo tidak balance ke nol.
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

        return raw
            .GroupBy(x => new { x.AccountId, x.AccountCode, x.AccountName, x.AccountType })
            .Select(g => new TrialBalanceRowDto
            {
                AccountId   = g.Key.AccountId,
                AccountCode = g.Key.AccountCode,
                AccountName = g.Key.AccountName,
                AccountType = g.Key.AccountType.ToString(),
                TotalDebit  = g.Sum(x => x.Debit),
                TotalCredit = g.Sum(x => x.Credit),
                Balance     = g.Sum(x => x.Debit) - g.Sum(x => x.Credit),
            })
            .OrderBy(r => r.AccountCode)
            .ToList();
    }

    public async Task<Guid> PostAsync(string description, JournalSourceType sourceType, Guid? sourceId, DateTimeOffset date, IReadOnlyList<PostingLine> lines)
    {
        if (lines.Count == 0)
            throw new ArgumentException("Journal posting harus punya minimal 1 baris.");

        var codes = lines.Select(l => l.AccountCode).Distinct().ToList();
        var accounts = await _db.Accounts.Where(a => codes.Contains(a.Code)).ToListAsync();

        var missingCodes = codes.Except(accounts.Select(a => a.Code)).ToList();
        if (missingCodes.Count > 0)
            throw new InvalidOperationException($"Account dengan Code {string.Join(", ", missingCodes)} tidak ditemukan.");

        var accountByCode = accounts.ToDictionary(a => a.Code);

        var totalDebit  = Math.Round(lines.Sum(l => l.Debit), 2);
        var totalCredit = Math.Round(lines.Sum(l => l.Credit), 2);
        if (totalDebit != totalCredit)
            throw new InvalidOperationException($"Journal entry tidak balance: Debit {totalDebit}, Credit {totalCredit}");

        var entryNumber = await NextEntryNumberAsync();

        var entry = new JournalEntry
        {
            EntryNumber = entryNumber,
            Date        = date,
            Description = description,
            SourceType  = sourceType,
            SourceId    = sourceId,
            Status      = JournalEntryStatus.Posted,
            PostedAt    = DateTimeOffset.UtcNow,
            Lines = lines.Select(l => new JournalEntryLine
            {
                AccountId = accountByCode[l.AccountCode].Id,
                Debit     = l.Debit,
                Credit    = l.Credit,
                Memo      = l.Memo,
            }).ToList(),
        };

        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync();
        return entry.Id;
    }

    private async Task<string> NextEntryNumberAsync()
    {
        var config = await _db.NumberingConfigs
            .FirstOrDefaultAsync(n => n.DocType == "JOURNAL_ENTRY")
            ?? throw new InvalidOperationException("NumberingConfig for JOURNAL_ENTRY not found");

        var no = config.GenerateNext();
        await _db.SaveChangesAsync();
        return no;
    }

    private static JournalEntryDto ToDto(JournalEntry x) => new()
    {
        Id                = x.Id,
        EntryNumber       = x.EntryNumber,
        Date              = x.Date,
        Description       = x.Description,
        SourceType        = x.SourceType.ToString(),
        SourceId          = x.SourceId,
        Status            = x.Status.ToString(),
        ReversedByEntryId = x.ReversedByEntryId,
        PostedAt          = x.PostedAt,
        TotalDebit        = x.Lines.Sum(l => l.Debit),
        TotalCredit       = x.Lines.Sum(l => l.Credit),
        CreatedAt         = x.CreatedAt,
        Lines = x.Lines.Select(l => new JournalEntryLineDto
        {
            Id          = l.Id,
            AccountId   = l.AccountId,
            AccountCode = l.Account?.Code ?? string.Empty,
            AccountName = l.Account?.Name ?? string.Empty,
            Debit       = l.Debit,
            Credit      = l.Credit,
            Memo        = l.Memo,
        }).ToList(),
    };
}
