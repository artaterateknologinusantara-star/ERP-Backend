using SynteraERP.Api.DTOs.SystemReset;

namespace SynteraERP.Api.Services.Interfaces;

public interface ISystemResetService
{
    Task<ResetResultDto> ResetQuotationsAsync(Guid userId, string? userIp);
    Task<ResetResultDto> ResetSalesAsync(Guid userId, string? userIp);
    Task<ResetResultDto> ResetPurchasingAsync(Guid userId, string? userIp);
    Task<ResetResultDto> ResetFinanceAsync(Guid userId, string? userIp);
    Task<ResetResultDto> ResetProjectsAsync(Guid userId, string? userIp);
    Task<ResetResultDto> ResetInventoryAsync(Guid userId, string? userIp);
    Task<ResetResultDto> ResetAllAsync(Guid userId, string? userIp);
}
