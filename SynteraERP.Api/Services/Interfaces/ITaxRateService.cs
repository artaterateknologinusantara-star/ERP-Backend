using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.TaxRate;

namespace SynteraERP.Api.Services.Interfaces;

public interface ITaxRateService
{
    Task<PaginatedResponse<TaxRateDto>> ListAsync(PaginationParams p);
    Task<TaxRateDto?> GetByIdAsync(Guid id);
    Task<TaxRateDto> CreateAsync(CreateTaxRateRequest request);
    Task<TaxRateDto?> UpdateAsync(Guid id, UpdateTaxRateRequest request);
    Task<bool> SetStatusAsync(Guid id, bool isActive);

    /// <summary>Rate (e.g. 0.11) of the active TaxRate flagged IsDefault, for use by other modules' posting/calculation logic.</summary>
    Task<decimal> GetDefaultRateAsync();
}
