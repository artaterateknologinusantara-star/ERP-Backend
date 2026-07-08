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
    Task<JournalEntryDto> CreateManualEntryAsync(CreateJournalEntryRequest request);

    /// <summary>
    /// Wrapper tipis di atas CreateManualEntryAsync, khusus SourceType=OpeningBalance. Validasi
    /// "tidak boleh menyentuh akun Revenue/Expense" sengaja diisolasi di sini (bukan di
    /// CreateManualEntryAsync) supaya method generik itu tidak menumpuk percabangan khusus
    /// per-SourceType (pola sama seperti keputusan desain GRNI di Fase 4) — dan supaya validasi ini
    /// tidak bisa "ketarik hilang" kalau CreateManualEntryAsync di-refactor nanti untuk keperluan lain.
    /// </summary>
    Task<JournalEntryDto> CreateOpeningBalanceAsync(CreateOpeningBalanceRequest request);

    Task<JournalEntryDto> ReverseAsync(Guid journalEntryId);
    Task<List<TrialBalanceRowDto>> GetTrialBalanceAsync(DateTimeOffset? asOfDate);

    /// <summary>
    /// Posting otomatis dari modul lain (Fase 2 dst.) - langsung Status=Posted, dipanggil di dalam
    /// transaction scope milik caller supaya rollback bareng kalau operasi bisnis aslinya gagal.
    /// </summary>
    Task<Guid> PostAsync(string description, JournalSourceType sourceType, Guid? sourceId, DateTimeOffset date, IReadOnlyList<PostingLine> lines);
}
