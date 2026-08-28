using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Supplier;
using SynteraERP.Api.Helpers;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class SupplierService : ISupplierService
{
    private readonly AppDbContext _db;

    public SupplierService(AppDbContext db) => _db = db;

    public async Task<PaginatedResponse<SupplierDto>> ListAsync(PaginationParams p)
    {
        var q = _db.Suppliers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(p.Search))
        {
            var s = p.Search.ToLower();
            q = q.Where(x => x.Name.ToLower().Contains(s)
                           || x.Code.ToLower().Contains(s)
                           || (x.City != null && x.City.ToLower().Contains(s))
                           || (x.ContactPerson != null && x.ContactPerson.ToLower().Contains(s)));
        }

        q = p.SortBy switch
        {
            "code"      => p.IsDescending ? q.OrderByDescending(x => x.Code) : q.OrderBy(x => x.Code),
            "city"      => p.IsDescending ? q.OrderByDescending(x => x.City) : q.OrderBy(x => x.City),
            "createdAt" => p.IsDescending ? q.OrderByDescending(x => x.CreatedAt) : q.OrderBy(x => x.CreatedAt),
            _           => p.IsDescending ? q.OrderByDescending(x => x.Name) : q.OrderBy(x => x.Name),
        };

        var total = await q.CountAsync();
        var data  = await q.Skip(p.Skip).Take(p.PerPage).Select(x => ToDto(x)).ToListAsync();
        return PaginatedResponse<SupplierDto>.Create(data, total, p.Page, p.PerPage);
    }

    public async Task<SupplierDto?> GetByIdAsync(Guid id)
    {
        var x = await _db.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        return x is null ? null : ToDto(x);
    }

    public Task<SupplierDto> CreateAsync(CreateSupplierRequest req) =>
        SequentialCodeHelper.RunWithRetryAsync(_db, async () =>
        {
            var supplier = new Supplier
            {
                Code          = await GenerateCodeAsync(),
                Name          = req.Name,
                ContactPerson = req.ContactPerson,
                Phone         = req.Phone,
                Email         = req.Email,
                Address       = req.Address,
                City          = req.City,
                Npwp          = req.Npwp,
                BankName      = req.BankName,
                BankAccount   = req.BankAccount,
                IsActive      = true,
            };
            _db.Suppliers.Add(supplier);
            await _db.SaveChangesAsync();
            return ToDto(supplier);
        });

    public async Task<SupplierDto?> UpdateAsync(Guid id, UpdateSupplierRequest req)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier is null) return null;

        supplier.Name          = req.Name;
        supplier.ContactPerson = req.ContactPerson;
        supplier.Phone         = req.Phone;
        supplier.Email         = req.Email;
        supplier.Address       = req.Address;
        supplier.City          = req.City;
        supplier.Npwp          = req.Npwp;
        supplier.BankName      = req.BankName;
        supplier.BankAccount   = req.BankAccount;
        supplier.UpdatedAt     = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return ToDto(supplier);
    }

    public async Task<bool> SetStatusAsync(Guid id, bool isActive)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier is null) return false;
        supplier.IsActive  = isActive;
        supplier.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier is null) return false;
        supplier.IsDeleted = true;
        supplier.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    private Task<string> GenerateCodeAsync()
    {
        return SequentialCodeHelper.NextCodeAsync(_db.Suppliers, "SUPP", 4);
    }

    private static SupplierDto ToDto(Supplier x) => new()
    {
        Id            = x.Id,
        Code          = x.Code,
        Name          = x.Name,
        ContactPerson = x.ContactPerson,
        Phone         = x.Phone,
        Email         = x.Email,
        Address       = x.Address,
        City          = x.City,
        Npwp          = x.Npwp,
        BankName      = x.BankName,
        BankAccount   = x.BankAccount,
        IsActive      = x.IsActive,
        CreatedAt     = x.CreatedAt,
    };
}
