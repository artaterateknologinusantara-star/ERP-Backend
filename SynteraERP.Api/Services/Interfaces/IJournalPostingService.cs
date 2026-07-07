using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.JournalEntry;

namespace SynteraERP.Api.Services.Interfaces;

public interface IJournalPostingService
{
    Task<PaginatedResponse<JournalEntryListDto>> ListAsync(JournalEntryQueryParams p);
    Task<JournalEntryDto?> GetByIdAsync(Guid id);
    Task<JournalEntryDto> CreateManualEntryAsync(CreateJournalEntryRequest request);
    Task<JournalEntryDto> ReverseAsync(Guid journalEntryId);
    Task<List<TrialBalanceRowDto>> GetTrialBalanceAsync(DateTimeOffset? asOfDate);
}
