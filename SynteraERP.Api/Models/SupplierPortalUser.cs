using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

// Login PIC untuk vendor/subcontractor — sengaja BUKAN User/Role internal (lihat Program.cs,
// scheme JWT "Vendor" terpisah). 1 Supplier bisa punya banyak PIC. Dibatasi di service layer
// (bukan di skema) ke Supplier.SupplierType Subcontractor/Both — lihat SupplierPortalUserService.
public class SupplierPortalUser : BaseEntity
{
    public Guid SupplierId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAt { get; set; }

    public Supplier Supplier { get; set; } = null!;
}
