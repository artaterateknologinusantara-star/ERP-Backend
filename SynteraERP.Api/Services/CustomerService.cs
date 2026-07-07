using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Customer;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _db;

    public CustomerService(AppDbContext db) => _db = db;

    public async Task<PaginatedResponse<CustomerDto>> ListAsync(CustomerParams p)
    {
        var q = _db.Customers.AsQueryable();

        if (p.IsActive.HasValue)
            q = q.Where(c => c.IsActive == p.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(p.Search))
        {
            var s = p.Search.ToLower();
            q = q.Where(c => c.Name.ToLower().Contains(s) || c.Code.ToLower().Contains(s) || (c.City != null && c.City.ToLower().Contains(s)));
        }

        q = p.SortBy switch
        {
            "code" => p.IsDescending ? q.OrderByDescending(c => c.Code) : q.OrderBy(c => c.Code),
            "city" => p.IsDescending ? q.OrderByDescending(c => c.City) : q.OrderBy(c => c.City),
            "createdAt" => p.IsDescending ? q.OrderByDescending(c => c.CreatedAt) : q.OrderBy(c => c.CreatedAt),
            _ => p.IsDescending ? q.OrderByDescending(c => c.Name) : q.OrderBy(c => c.Name),
        };

        var total = await q.CountAsync();
        var data = await q.Skip(p.Skip).Take(p.PerPage)
            .Select(c => ToDto(c))
            .ToListAsync();

        return PaginatedResponse<CustomerDto>.Create(data, total, p.Page, p.PerPage);
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id)
    {
        var c = await _db.Customers.FindAsync(id);
        return c is null ? null : ToDto(c);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request)
    {
        var code = await GenerateCodeAsync();
        var customer = new Customer
        {
            Code = code,
            Name = request.Name,
            Industry = request.Industry,
            ContactPerson = request.ContactPerson,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            City = request.City,
            Npwp = request.Npwp,
            IsActive = true,
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return ToDto(customer);
    }

    public async Task<CustomerDto?> UpdateAsync(Guid id, UpdateCustomerRequest request)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null) return null;

        customer.Name = request.Name;
        customer.Industry = request.Industry;
        customer.ContactPerson = request.ContactPerson;
        customer.Phone = request.Phone;
        customer.Email = request.Email;
        customer.Address = request.Address;
        customer.City = request.City;
        customer.Npwp = request.Npwp;
        customer.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return ToDto(customer);
    }

    public async Task<bool> SetStatusAsync(Guid id, bool isActive)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null) return false;

        customer.IsActive = isActive;
        customer.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null) return false;

        customer.IsDeleted = true;
        customer.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<string> GenerateCodeAsync()
    {
        var count = await _db.Customers.CountAsync() + 1;
        return $"CUST{count:D4}";
    }

    private static CustomerDto ToDto(Customer c) => new()
    {
        Id = c.Id,
        Code = c.Code,
        Name = c.Name,
        Industry = c.Industry,
        ContactPerson = c.ContactPerson,
        Phone = c.Phone,
        Email = c.Email,
        Address = c.Address,
        City = c.City,
        Npwp = c.Npwp,
        IsActive = c.IsActive,
        CreatedAt = c.CreatedAt,
    };
}
