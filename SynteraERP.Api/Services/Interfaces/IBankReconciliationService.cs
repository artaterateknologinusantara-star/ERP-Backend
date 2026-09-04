using Microsoft.AspNetCore.Http;
using SynteraERP.Api.DTOs.BankReconciliation;

namespace SynteraERP.Api.Services.Interfaces;

public interface IBankReconciliationService
{
    Task<BankStatementImportResult> ImportAsync(ImportBankStatementRequest request, IFormFile file);
    Task<List<BankStatementImportListDto>> ListImportsAsync(Guid accountId);
    Task<BankStatementImportDetailDto?> GetImportDetailAsync(Guid id);
    Task<BankStatementLineDetailDto> MatchAsync(Guid lineId, Guid journalEntryLineId);
    Task<BankStatementLineDetailDto> UnmatchAsync(Guid lineId);
    Task<BankStatementLineDetailDto> IgnoreAsync(Guid lineId);
    Task<List<AccountBalanceDto>> GetBalancesAsync(DateOnly asOf);
}
