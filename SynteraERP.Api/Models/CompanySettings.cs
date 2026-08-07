namespace SynteraERP.Api.Models;

public class CompanySettings
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CompanyName { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public string? LogoFileName { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? FooterText { get; set; }
    public string? SignatureName { get; set; }
    public string? SignatureTitle { get; set; }
    public string? DocumentPrefix { get; set; }
    public string? Npwp { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankAccountHolderName { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
