namespace SynteraERP.Api.Models;

public class CompanySettings
{
    public Guid Id { get; set; } = Guid.NewGuid();
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
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
