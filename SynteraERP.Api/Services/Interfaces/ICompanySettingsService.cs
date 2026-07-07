using SynteraERP.Api.DTOs.CompanySettings;

namespace SynteraERP.Api.Services.Interfaces;

public interface ICompanySettingsService
{
    Task<CompanySettingsDto> GetAsync();
    Task<CompanySettingsDto> UpdateAsync(UpdateCompanySettingsRequest request);
}
