using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.VendorPortal;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class SupplierPortalUserService : ISupplierPortalUserService
{
    private readonly AppDbContext _db;

    public SupplierPortalUserService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<SupplierPortalUserDto>> ListBySupplierAsync(Guid supplierId)
    {
        return await _db.SupplierPortalUsers
            .AsNoTracking()
            .Where(u => u.SupplierId == supplierId)
            .OrderBy(u => u.Name)
            .Select(u => ToDto(u))
            .ToListAsync();
    }

    public async Task<SupplierPortalUserDto> CreateAsync(Guid supplierId, CreateSupplierPortalUserRequest request)
    {
        var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId)
            ?? throw new KeyNotFoundException("Supplier tidak ditemukan.");

        // Keputusan produk 24 Sep 2026: portal RAB hanya untuk vendor yang bisa berperan sebagai
        // subkontraktor — dicek di sini (application layer), bukan di skema, karena SupplierType
        // adalah klasifikasi lunak (nullable enum), bukan tabel terpisah.
        if (supplier.SupplierType is not (SupplierType.Subcontractor or SupplierType.Both))
            throw new InvalidOperationException(
                "Akun portal vendor hanya bisa dibuat untuk Supplier bertipe Subcontractor atau Both.");

        var emailTaken = await _db.SupplierPortalUsers
            .AnyAsync(u => u.SupplierId == supplierId && u.Email == request.Email);
        if (emailTaken)
            throw new InvalidOperationException("Email ini sudah dipakai PIC lain untuk vendor yang sama.");

        var portalUser = new SupplierPortalUser
        {
            SupplierId = supplierId,
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        };
        _db.SupplierPortalUsers.Add(portalUser);
        await _db.SaveChangesAsync();

        return ToDto(portalUser);
    }

    public async Task<bool> SetActiveAsync(Guid id, bool isActive)
    {
        var portalUser = await _db.SupplierPortalUsers.FirstOrDefaultAsync(u => u.Id == id);
        if (portalUser is null) return false;

        portalUser.IsActive = isActive;
        await _db.SaveChangesAsync();
        return true;
    }

    private static SupplierPortalUserDto ToDto(SupplierPortalUser u) => new()
    {
        Id = u.Id,
        SupplierId = u.SupplierId,
        Name = u.Name,
        Email = u.Email,
        IsActive = u.IsActive,
        LastLoginAt = u.LastLoginAt,
        CreatedAt = u.CreatedAt,
    };
}
