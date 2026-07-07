using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.TaxRate;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class TaxRateService : ITaxRateService
{
    private readonly AppDbContext _db;

    public TaxRateService(AppDbContext db) => _db = db;

    public async Task<PaginatedResponse<TaxRateDto>> ListAsync(PaginationParams p)
    {
        var q = _db.TaxRates.AsQueryable();

        if (!string.IsNullOrWhiteSpace(p.Search))
        {
            var s = p.Search.ToLower();
            q = q.Where(x => x.Name.ToLower().Contains(s) || x.Code.ToLower().Contains(s));
        }

        q = p.SortBy switch
        {
            "code"      => p.IsDescending ? q.OrderByDescending(x => x.Code) : q.OrderBy(x => x.Code),
            "rate"      => p.IsDescending ? q.OrderByDescending(x => x.Rate) : q.OrderBy(x => x.Rate),
            "createdAt" => p.IsDescending ? q.OrderByDescending(x => x.CreatedAt) : q.OrderBy(x => x.CreatedAt),
            _           => p.IsDescending ? q.OrderByDescending(x => x.Name) : q.OrderBy(x => x.Name),
        };

        var total = await q.CountAsync();
        var data  = await q.Skip(p.Skip).Take(p.PerPage).Select(x => ToDto(x)).ToListAsync();
        return PaginatedResponse<TaxRateDto>.Create(data, total, p.Page, p.PerPage);
    }

    public async Task<TaxRateDto?> GetByIdAsync(Guid id)
    {
        var x = await _db.TaxRates.FindAsync(id);
        return x is null ? null : ToDto(x);
    }

    public async Task<TaxRateDto> CreateAsync(CreateTaxRateRequest req)
    {
        if (req.IsDefault)
            await ClearDefaultsAsync();

        var taxRate = new TaxRate
        {
            Code          = req.Code,
            Name          = req.Name,
            Rate          = req.Rate,
            IsDefault     = req.IsDefault,
            EffectiveFrom = req.EffectiveFrom,
            EffectiveTo   = req.EffectiveTo,
            IsActive      = true,
        };
        _db.TaxRates.Add(taxRate);
        await _db.SaveChangesAsync();
        return ToDto(taxRate);
    }

    public async Task<TaxRateDto?> UpdateAsync(Guid id, UpdateTaxRateRequest req)
    {
        var taxRate = await _db.TaxRates.FindAsync(id);
        if (taxRate is null) return null;

        if (req.IsDefault && !taxRate.IsDefault)
            await ClearDefaultsAsync();

        taxRate.Code          = req.Code;
        taxRate.Name          = req.Name;
        taxRate.Rate          = req.Rate;
        taxRate.IsDefault     = req.IsDefault;
        taxRate.EffectiveFrom = req.EffectiveFrom;
        taxRate.EffectiveTo   = req.EffectiveTo;
        taxRate.UpdatedAt     = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return ToDto(taxRate);
    }

    public async Task<bool> SetStatusAsync(Guid id, bool isActive)
    {
        var taxRate = await _db.TaxRates.FindAsync(id);
        if (taxRate is null) return false;
        taxRate.IsActive  = isActive;
        taxRate.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<decimal> GetDefaultRateAsync()
    {
        var rate = await _db.TaxRates
            .Where(x => x.IsDefault && x.IsActive)
            .Select(x => (decimal?)x.Rate)
            .FirstOrDefaultAsync();

        return rate ?? throw new InvalidOperationException("Tidak ada TaxRate default yang aktif.");
    }

    private async Task ClearDefaultsAsync()
    {
        var currentDefaults = await _db.TaxRates.Where(x => x.IsDefault).ToListAsync();
        foreach (var x in currentDefaults)
        {
            x.IsDefault = false;
            x.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    private static TaxRateDto ToDto(TaxRate x) => new()
    {
        Id            = x.Id,
        Code          = x.Code,
        Name          = x.Name,
        Rate          = x.Rate,
        IsDefault     = x.IsDefault,
        EffectiveFrom = x.EffectiveFrom,
        EffectiveTo   = x.EffectiveTo,
        IsActive      = x.IsActive,
        CreatedAt     = x.CreatedAt,
    };
}
