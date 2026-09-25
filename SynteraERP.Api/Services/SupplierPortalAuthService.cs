using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.VendorPortal;
using SynteraERP.Api.Helpers;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class SupplierPortalAuthService : ISupplierPortalAuthService
{
    private readonly AppDbContext _db;
    private readonly VendorJwtHelper _jwt;

    public SupplierPortalAuthService(AppDbContext db, VendorJwtHelper jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public async Task<VendorLoginResponse?> LoginAsync(VendorLoginRequest request)
    {
        var portalUser = await _db.SupplierPortalUsers
            .Include(u => u.Supplier)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

        if (portalUser is null || !BCrypt.Net.BCrypt.Verify(request.Password, portalUser.PasswordHash))
            return null;

        // Supplier bisa saja di-nonaktifkan/soft-delete setelah akun portal dibuat — akun tidak
        // otomatis nonaktif mengikuti Supplier-nya, jadi dicek eksplisit di sini juga.
        if (!portalUser.Supplier.IsActive)
            return null;

        portalUser.LastLoginAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        var (token, expiresAt) = _jwt.Generate(portalUser);

        return new VendorLoginResponse
        {
            Token = token,
            Name = portalUser.Name,
            Email = portalUser.Email,
            SupplierId = portalUser.SupplierId,
            SupplierName = portalUser.Supplier.Name,
            ExpiresAt = expiresAt,
        };
    }
}
