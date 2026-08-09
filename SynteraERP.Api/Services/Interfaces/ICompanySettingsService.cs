using Microsoft.AspNetCore.Http;
using SynteraERP.Api.DTOs.CompanySettings;

namespace SynteraERP.Api.Services.Interfaces;

public interface ICompanySettingsService
{
    Task<CompanySettingsDto> GetAsync();
    Task<PublicCompanySettingsDto> GetPublicAsync();
    Task<CompanySettingsDto> UpdateAsync(UpdateCompanySettingsRequest request);
    Task<CompanySettingsDto> UploadLogoAsync(IFormFile file);
    Task<(byte[] data, string contentType, string fileName)?> GetLogoAsync();
    Task<CompanySettingsDto> DeleteLogoAsync();
    Task<RegeneratePrefixesResponse> RegeneratePrefixesAsync();
}
