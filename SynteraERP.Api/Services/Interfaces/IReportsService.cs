using SynteraERP.Api.DTOs.JournalEntry;
using SynteraERP.Api.DTOs.Reports;

namespace SynteraERP.Api.Services.Interfaces;

public interface IReportsService
{
    Task<List<TrialBalanceRowDto>> GetTrialBalanceAsync(DateOnly? asOfDate);
    Task<IncomeStatementDto> GetIncomeStatementAsync(DateOnly? startDate, DateOnly? endDate);
    Task<BalanceSheetDto> GetBalanceSheetAsync(DateOnly? asOfDate);
    Task<GeneralLedgerDto?> GetGeneralLedgerAsync(Guid accountId, DateOnly? startDate, DateOnly? endDate);
}
