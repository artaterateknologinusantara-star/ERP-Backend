using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.JournalEntry;
using SynteraERP.Api.Models;

namespace SynteraERP.Api.Services.Interfaces;

/// <summary>Satu baris posting otomatis, diidentifikasi lewat Account.Code (bukan Guid) supaya caller tidak perlu query Account sendiri.</summary>
public record PostingLine(string AccountCode, decimal Debit, decimal Credit, string? Memo = null);

public interface IJournalPostingService
{
    Task<PaginatedResponse<JournalEntryListDto>> ListAsync(JournalEntryQueryParams p);
    Task<JournalEntryDto?> GetByIdAsync(Guid id);
    Task<JournalEntryDto> CreateManualEntryAsync(CreateJournalEntryRequest request, Guid createdByUserId);

    /// <summary>
    /// Wrapper tipis di atas CreateManualEntryAsync, khusus SourceType=OpeningBalance. Validasi
    /// "tidak boleh menyentuh akun Revenue/Expense" sengaja diisolasi di sini (bukan di
    /// CreateManualEntryAsync) supaya method generik itu tidak menumpuk percabangan khusus
    /// per-SourceType (pola sama seperti keputusan desain GRNI di Fase 4) — dan supaya validasi ini
    /// tidak bisa "ketarik hilang" kalau CreateManualEntryAsync di-refactor nanti untuk keperluan lain.
    /// </summary>
    Task<JournalEntryDto> CreateOpeningBalanceAsync(CreateOpeningBalanceRequest request, Guid createdByUserId);

    /// <summary>
    /// Transisi Draft → Posted untuk journal entry manual yang sudah dibuat (Segregation of Duties:
    /// pembuat dan penyetuju wajib beda step, digate PermissionActions.Approve di controller). Beda
    /// dengan PostAsync di bawah, yang membuat entry BARU langsung Posted untuk auto-posting modul
    /// lain.
    /// </summary>
    Task<JournalEntryDto> PostDraftEntryAsync(Guid id, Guid postedByUserId);

    Task<JournalEntryDto> ReverseAsync(Guid journalEntryId, Guid reversedByUserId);
    Task<List<TrialBalanceRowDto>> GetTrialBalanceAsync(DateTimeOffset? asOfDate);

    /// <summary>
    /// Posting otomatis dari modul lain (Fase 2 dst.) - langsung Status=Posted, dipanggil di dalam
    /// transaction scope milik caller supaya rollback bareng kalau operasi bisnis aslinya gagal.
    /// </summary>
    Task<Guid> PostAsync(string description, JournalSourceType sourceType, Guid? sourceId, DateTimeOffset date, IReadOnlyList<PostingLine> lines);
}
