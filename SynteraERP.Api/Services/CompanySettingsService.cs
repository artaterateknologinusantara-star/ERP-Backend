using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.CompanySettings;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class CompanySettingsService : ICompanySettingsService
{
    private readonly AppDbContext _db;

    public CompanySettingsService(AppDbContext db) => _db = db;

    public async Task<CompanySettingsDto> GetAsync()
    {
        var settings = await _db.CompanySettings.FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("CompanySettings belum ter-seed.");
        return ToDto(settings);
    }

    public async Task<CompanySettingsDto> UpdateAsync(UpdateCompanySettingsRequest req)
    {
        var settings = await _db.CompanySettings.FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("CompanySettings belum ter-seed.");

        settings.CompanyName    = req.CompanyName;
        settings.LogoPath       = req.LogoPath;
        settings.Address        = req.Address;
        settings.Phone          = req.Phone;
        settings.Email          = req.Email;
        settings.Website        = req.Website;
        settings.FooterText     = req.FooterText;
        settings.SignatureName  = req.SignatureName;
        settings.SignatureTitle = req.SignatureTitle;
        settings.UpdatedAt      = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return ToDto(settings);
    }

    private static CompanySettingsDto ToDto(Models.CompanySettings x) => new()
    {
        Id              = x.Id,
        CompanyName     = x.CompanyName,
        LogoPath        = x.LogoPath,
        Address         = x.Address,
        Phone           = x.Phone,
        Email           = x.Email,
        Website         = x.Website,
        FooterText      = x.FooterText,
        SignatureName   = x.SignatureName,
        SignatureTitle  = x.SignatureTitle,
        UpdatedAt       = x.UpdatedAt,
    };
}
