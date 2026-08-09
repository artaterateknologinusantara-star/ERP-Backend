using System.ComponentModel.DataAnnotations;

namespace SynteraERP.Api.DTOs.CompanySettings;

public class CompanySettingsDto
{
    public Guid Id { get; set; }
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
    public DateTimeOffset UpdatedAt { get; set; }
}

// Deliberately minimal — served anonymously (login page, browser tab title). Must never grow to
// include anything sensitive (email, NPWP, bank details, etc.) that the full CompanySettingsDto has.
public class PublicCompanySettingsDto
{
    public string CompanyName { get; set; } = string.Empty;
    public bool HasLogo { get; set; }
}

public class NumberingConfigDto
{
    public string DocType { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public int LastNumber { get; set; }
}

public class RegeneratePrefixesResponse
{
    public int UpdatedCount { get; set; }
    public List<NumberingConfigDto> NumberingConfigs { get; set; } = [];
}

// Logo is managed exclusively via the dedicated upload/delete logo endpoints (multipart), not here —
// keeps this request pure JSON and stops callers from setting an arbitrary LogoPath/LogoFileName
// string that never went through the upload validation.
public class UpdateCompanySettingsRequest
{
    [Required(ErrorMessage = "Nama Perusahaan wajib diisi.")]
    [StringLength(200, ErrorMessage = "Nama Perusahaan maksimal 200 karakter.")]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Alamat maksimal 500 karakter.")]
    public string? Address { get; set; }

    [StringLength(50, ErrorMessage = "Telepon maksimal 50 karakter.")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Format Email tidak valid.")]
    [StringLength(150, ErrorMessage = "Email maksimal 150 karakter.")]
    public string? Email { get; set; }

    [StringLength(200, ErrorMessage = "Website maksimal 200 karakter.")]
    public string? Website { get; set; }

    [StringLength(1000, ErrorMessage = "Footer Text maksimal 1000 karakter.")]
    public string? FooterText { get; set; }

    [StringLength(100, ErrorMessage = "Nama Penanda Tangan maksimal 100 karakter.")]
    public string? SignatureName { get; set; }

    [StringLength(100, ErrorMessage = "Jabatan Penanda Tangan maksimal 100 karakter.")]
    public string? SignatureTitle { get; set; }

    [StringLength(20, ErrorMessage = "Kode/Prefix Dokumen maksimal 20 karakter.")]
    public string? DocumentPrefix { get; set; }

    [StringLength(20, ErrorMessage = "NPWP maksimal 20 karakter.")]
    public string? Npwp { get; set; }

    [StringLength(100, ErrorMessage = "Nama Bank maksimal 100 karakter.")]
    public string? BankName { get; set; }

    [StringLength(50, ErrorMessage = "Nomor Rekening maksimal 50 karakter.")]
    public string? BankAccountNumber { get; set; }

    [StringLength(100, ErrorMessage = "Nama Pemilik Rekening maksimal 100 karakter.")]
    public string? BankAccountHolderName { get; set; }
}
