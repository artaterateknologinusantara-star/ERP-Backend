using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.ItemMaster;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class ItemMasterService : IItemMasterService
{
    private readonly AppDbContext _db;

    public ItemMasterService(AppDbContext db) => _db = db;

    public async Task<PaginatedResponse<ItemMasterDto>> ListAsync(ItemMasterParams p)
    {
        var q = _db.ItemMasters.AsQueryable();

        if (p.IsActive.HasValue)
            q = q.Where(i => i.IsActive == p.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(p.Search))
        {
            var search = p.Search.ToLower();
            q = q.Where(i => i.Code.ToLower().Contains(search)
                         || i.Name.ToLower().Contains(search)
                         || (i.Category != null && i.Category.ToLower().Contains(search))
                         || (i.Brand != null && i.Brand.ToLower().Contains(search))
                         || (i.Warehouse != null && i.Warehouse.ToLower().Contains(search)));
        }

        q = p.SortBy switch
        {
            "code" => p.IsDescending ? q.OrderByDescending(i => i.Code) : q.OrderBy(i => i.Code),
            "name" => p.IsDescending ? q.OrderByDescending(i => i.Name) : q.OrderBy(i => i.Name),
            "stock" => p.IsDescending ? q.OrderByDescending(i => i.Stock) : q.OrderBy(i => i.Stock),
            "price" => p.IsDescending ? q.OrderByDescending(i => i.Price) : q.OrderBy(i => i.Price),
            "createdAt" => p.IsDescending ? q.OrderByDescending(i => i.CreatedAt) : q.OrderBy(i => i.CreatedAt),
            _ => p.IsDescending ? q.OrderByDescending(i => i.Code) : q.OrderBy(i => i.Code),
        };

        var total = await q.CountAsync();
        var data = await q.Skip(p.Skip).Take(p.PerPage)
            .Select(i => ToDto(i))
            .ToListAsync();

        return PaginatedResponse<ItemMasterDto>.Create(data, total, p.Page, p.PerPage);
    }

    public async Task<ItemMasterDto?> GetByIdAsync(Guid id)
    {
        var item = await _db.ItemMasters.FindAsync(id);
        return item is null ? null : ToDto(item);
    }

    public async Task<ItemMasterStatsDto> GetStatsAsync()
    {
        var totalAll = await _db.ItemMasters.CountAsync();
        var totalActive = await _db.ItemMasters.CountAsync(i => i.IsActive);
        var lowStockCount = await _db.ItemMasters.CountAsync(i => i.IsActive && i.Stock <= i.MinStock);
        return new ItemMasterStatsDto
        {
            TotalAll = totalAll,
            TotalActive = totalActive,
            LowStockCount = lowStockCount,
        };
    }

    private static ItemMasterDto ToDto(ItemMaster item) => new()
    {
        Id = item.Id,
        Code = item.Code,
        Name = item.Name,
        Description = item.Description,
        Category = item.Category,
        Brand = item.Brand,
        Uom = item.Uom,
        Warehouse = item.Warehouse,
        Stock = item.Stock,
        MinStock = item.MinStock,
        Price = item.Price,
        IsActive = item.IsActive,
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt,
    };
}
