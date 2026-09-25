using System.ComponentModel.DataAnnotations;

namespace SynteraERP.Api.DTOs.VendorPortal;

public class SupplierPortalUserDto
{
    public Guid Id { get; set; }
    public Guid SupplierId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class CreateSupplierPortalUserRequest
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    // Diisi admin internal langsung (belum ada infra kirim email/undangan) — vendor diberi tahu
    // password ini di luar sistem, sama seperti pola forgot-password admin-relay yang sudah ada.
    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;
}
