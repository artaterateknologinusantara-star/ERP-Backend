using System.ComponentModel.DataAnnotations;

namespace SynteraERP.Api.DTOs.CompanySettings;

public class CompanySettingsDto
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? FooterText { get; set; }
    public string? SignatureName { get; set; }
    public string? SignatureTitle { get; set; }
    public string? DocumentPrefix { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class UpdateCompanySettingsRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? FooterText { get; set; }
    public string? SignatureName { get; set; }
    public string? SignatureTitle { get; set; }

    [StringLength(20, ErrorMessage = "Kode/Prefix Dokumen maksimal 20 karakter.")]
    public string? DocumentPrefix { get; set; }
}
