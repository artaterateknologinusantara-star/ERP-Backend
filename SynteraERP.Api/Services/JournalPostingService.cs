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
        var q = _db.JournalEntries.AsNoTracking().Include(x => x.Lines).AsQueryable();

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
                CreatedByName = _db.Users.Where(u => u.Id == x.CreatedBy).Select(u => u.Name).FirstOrDefault(),
                TotalDebit = x.Lines.Sum(l => l.Debit),
                TotalCredit = x.Lines.Sum(l => l.Credit),
            })
            .ToListAsync();

        return PaginatedResponse<JournalEntryListDto>.Create(data, total, p.Page, p.PerPage);
    }

    public async Task<JournalEntryDto?> GetByIdAsync(Guid id)
    {
        var entry = await _db.JournalEntries
            .AsNoTracking()
            .Include(x => x.Lines)
                .ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entry is null) return null;

        string? postedByName = null;
        if (entry.PostedByUserId.HasValue)
        {
            var poster = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == entry.PostedByUserId.Value);
            postedByName = poster?.Name;
        }

        string? createdByName = null;
        if (entry.CreatedBy.HasValue)
        {
            var creator = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == entry.CreatedBy.Value);
            createdByName = creator?.Name;
        }

        return ToDto(entry, postedByName, createdByName);
    }

    public async Task<JournalEntryDto> CreateManualEntryAsync(CreateJournalEntryRequest req, Guid createdByUserId)
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

        // Segregation of Duties: create selalu berhenti di Draft, terlepas dari
        // req.PostImmediately (field dipertahankan di DTO untuk backward-compat, tapi nilainya
        // diabaikan di sini) — posting jadi langkah terpisah lewat PostDraftEntryAsync(id, postedByUserId),
        // digate permission Approve di controller supaya pembuat dan penyetuju wajib beda user.
        var entry = new JournalEntry
        {
            EntryNumber = entryNumber,
            Date        = req.Date ?? DateTimeOffset.UtcNow,
            Description = req.Description,
            SourceType  = sourceType,
            SourceId    = req.SourceId,
            Status      = JournalEntryStatus.Draft,
            PostedAt    = null,
            PostedByUserId = null,
            CreatedBy   = createdByUserId,
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

    public async Task<JournalEntryDto> CreateOpeningBalanceAsync(CreateOpeningBalanceRequest req, Guid createdByUserId)
    {
        if (req.Lines.Count == 0)
            throw new ArgumentException("Opening Balance harus punya minimal 1 baris.");

        var accountIds = req.Lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await _db.Accounts.Where(a => accountIds.Contains(a.Id)).ToListAsync();

        var missingIds = accountIds.Except(accounts.Select(a => a.Id)).ToList();
        if (missingIds.Count > 0)
            throw new InvalidOperationException($"Account dengan Id {string.Join(", ", missingIds)} tidak ditemukan.");

        var accountById = accounts.ToDictionary(a => a.Id);

        // Opening Balance HANYA untuk akun Neraca (Asset/Liability/Equity) — saldo dari pembukuan lama
        // tidak pernah berbentuk Pendapatan/Beban. Kalau baris Revenue/Expense lolos ke sini, Laba Rugi
        // (GetIncomeStatementAsync) dan baris "Laba Rugi Berjalan (Belum Ditutup)" di Neraca
        // (GetBalanceSheetAsync) akan terdistorsi permanen sejak hari pertama go-live. Validasi ini
        // sengaja diletakkan di wrapper ini, bukan di CreateManualEntryAsync — lihat catatan di
        // IJournalPostingService.CreateOpeningBalanceAsync.
        var invalidLines = req.Lines
            .Where(l => accountById[l.AccountId].Type is AccountType.Revenue or AccountType.Expense)
            .Select(l => accountById[l.AccountId].Code)
            .Distinct()
            .ToList();

        if (invalidLines.Count > 0)
            throw new InvalidOperationException(
                $"Opening Balance tidak boleh menyentuh akun Pendapatan/Beban: {string.Join(", ", invalidLines)}. " +
                "Saldo awal hanya berlaku untuk akun Asset/Liability/Equity.");

        // KETERBATASAN DIKETAHUI: tidak ada lock tanggal (Period Closing/#13 di roadmap belum
        // dikerjakan) — setelah Opening Balance ini di-Posted, JE manual lain masih bisa dibuat dengan
        // Date sebelum cut-off ini, yang bisa mengubah saldo "historis" yang seharusnya sudah final.
        // Diterima sebagai keterbatasan sementara (lihat 00_PROJECT_STATUS.md Known Gaps), akan
        // ditutup permanen oleh Period Closing.
        //
        // PERUBAHAN PERILAKU (SoD): Opening Balance sekarang juga berhenti di Draft seperti JE manual
        // lain — perlu di-post manual lewat PostDraftEntryAsync sesudahnya, tidak lagi otomatis Posted.
        return await CreateManualEntryAsync(new CreateJournalEntryRequest
        {
            Date = req.Date,
            Description = string.IsNullOrWhiteSpace(req.Description) ? "Opening Balance" : req.Description,
            SourceType = nameof(JournalSourceType.OpeningBalance),
            Lines = req.Lines,
        }, createdByUserId);
    }

    /// <summary>
    /// Transisi Draft → Posted untuk journal entry manual (Segregation of Duties: langkah terpisah
    /// dari CreateManualEntryAsync, digate permission Approve di controller).
    /// </summary>
    public async Task<JournalEntryDto> PostDraftEntryAsync(Guid id, Guid postedByUserId)
    {
        var entry = await _db.JournalEntries.FindAsync(id)
            ?? throw new KeyNotFoundException("Journal entry tidak ditemukan.");

        if (entry.Status != JournalEntryStatus.Draft)
            throw new InvalidOperationException("Hanya entry berstatus Draft yang bisa di-post.");

        entry.Status        = JournalEntryStatus.Posted;
        entry.PostedAt      = DateTimeOffset.UtcNow;
        entry.PostedByUserId = postedByUserId;
        entry.UpdatedAt     = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return (await GetByIdAsync(entry.Id))!;
    }

    public async Task<JournalEntryDto> ReverseAsync(Guid journalEntryId, Guid reversedByUserId)
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
            PostedByUserId = reversedByUserId,
            CreatedBy   = reversedByUserId,
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
            .AsNoTracking()
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

    private static JournalEntryDto ToDto(JournalEntry x, string? postedByName = null, string? createdByName = null) => new()
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
        PostedByName      = postedByName,
        CreatedByName     = createdByName,
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
