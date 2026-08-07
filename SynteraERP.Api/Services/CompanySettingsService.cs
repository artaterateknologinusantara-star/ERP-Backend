using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.CompanySettings;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class CompanySettingsService : ICompanySettingsService
{
    private static readonly string[] AllowedLogoExtensions = [".png", ".jpg", ".jpeg", ".gif", ".webp"];
    private const long MaxLogoSizeBytes = 2 * 1024 * 1024; // 2 MB

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public CompanySettingsService(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

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

        // Defense-in-depth: DTO's [Required] already blocks this via ModelState, but this guard
        // stays correct even if UpdateAsync is ever called from somewhere that skips ModelState
        // (the DB column's NOT NULL alone lets "" through silently, which is not good enough).
        if (string.IsNullOrWhiteSpace(req.CompanyName))
            throw new ArgumentException("Nama Perusahaan wajib diisi dan tidak boleh kosong.");

        settings.CompanyName            = req.CompanyName.Trim();
        settings.Address                = req.Address;
        settings.Phone                  = req.Phone;
        settings.Email                  = req.Email;
        settings.Website                = req.Website;
        settings.FooterText             = req.FooterText;
        settings.SignatureName          = req.SignatureName;
        settings.SignatureTitle         = req.SignatureTitle;
        settings.DocumentPrefix         = req.DocumentPrefix;
        settings.Npwp                   = req.Npwp;
        settings.BankName               = req.BankName;
        settings.BankAccountNumber      = req.BankAccountNumber;
        settings.BankAccountHolderName  = req.BankAccountHolderName;
        settings.UpdatedAt              = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return ToDto(settings);
    }

    public async Task<CompanySettingsDto> UploadLogoAsync(IFormFile file)
    {
        if (file.Length == 0)
            throw new ArgumentException("File logo kosong.");
        if (file.Length > MaxLogoSizeBytes)
            throw new ArgumentException("Ukuran file logo maksimal 2 MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedLogoExtensions.Contains(ext))
            throw new ArgumentException("Format file logo harus PNG, JPG, GIF, atau WEBP.");

        var settings = await _db.CompanySettings.FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("CompanySettings belum ter-seed.");

        var uploadsDir = Path.Combine(_env.ContentRootPath, "uploads", "company-logo");
        Directory.CreateDirectory(uploadsDir);

        var storedName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(uploadsDir, storedName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
            await file.CopyToAsync(stream);

        // Remove the previous logo file from disk now that the new one is safely written
        if (settings.LogoPath is not null)
        {
            var oldFullPath = Path.Combine(_env.ContentRootPath, "uploads", settings.LogoPath);
            if (File.Exists(oldFullPath)) File.Delete(oldFullPath);
        }

        settings.LogoPath = Path.Combine("company-logo", storedName);
        settings.LogoFileName = file.FileName;
        settings.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        return ToDto(settings);
    }

    public async Task<(byte[] data, string contentType, string fileName)?> GetLogoAsync()
    {
        var settings = await _db.CompanySettings.FirstOrDefaultAsync();
        if (settings?.LogoPath is null) return null;

        var fullPath = Path.Combine(_env.ContentRootPath, "uploads", settings.LogoPath);
        if (!File.Exists(fullPath)) return null;

        var data = await File.ReadAllBytesAsync(fullPath);
        var contentType = GetContentType(settings.LogoPath);
        var fileName = settings.LogoFileName ?? Path.GetFileName(settings.LogoPath);
        return (data, contentType, fileName);
    }

    public async Task<CompanySettingsDto> DeleteLogoAsync()
    {
        var settings = await _db.CompanySettings.FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("CompanySettings belum ter-seed.");

        if (settings.LogoPath is not null)
        {
            var fullPath = Path.Combine(_env.ContentRootPath, "uploads", settings.LogoPath);
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }

        settings.LogoPath = null;
        settings.LogoFileName = null;
        settings.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        return ToDto(settings);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static string GetContentType(string path) =>
        Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream",
        };

    private static CompanySettingsDto ToDto(Models.CompanySettings x) => new()
    {
        Id                      = x.Id,
        CompanyName             = x.CompanyName,
        LogoPath                = x.LogoPath,
        LogoFileName            = x.LogoFileName,
        Address                 = x.Address,
        Phone                   = x.Phone,
        Email                   = x.Email,
        Website                 = x.Website,
        FooterText              = x.FooterText,
        SignatureName           = x.SignatureName,
        SignatureTitle          = x.SignatureTitle,
        DocumentPrefix          = x.DocumentPrefix,
        Npwp                    = x.Npwp,
        BankName                = x.BankName,
        BankAccountNumber       = x.BankAccountNumber,
        BankAccountHolderName   = x.BankAccountHolderName,
        UpdatedAt               = x.UpdatedAt,
    };
}
