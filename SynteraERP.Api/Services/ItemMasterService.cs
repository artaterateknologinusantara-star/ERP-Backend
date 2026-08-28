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
        var q = _db.ItemMasters.Include(i => i.PreferredVendor).AsQueryable();

        if (p.IsActive.HasValue)
            q = q.Where(i => i.IsActive == p.IsActive.Value);

        if (p.BelowMinimumMargin == true)
            q = ApplyBelowMinimumMarginFilter(q);

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
            "code"          => p.IsDescending ? q.OrderByDescending(i => i.Code)          : q.OrderBy(i => i.Code),
            "name"          => p.IsDescending ? q.OrderByDescending(i => i.Name)          : q.OrderBy(i => i.Name),
            "stock"         => p.IsDescending ? q.OrderByDescending(i => i.Stock)         : q.OrderBy(i => i.Stock),
            "price"         => p.IsDescending ? q.OrderByDescending(i => i.SellingPrice)  : q.OrderBy(i => i.SellingPrice),
            "sellingPrice"  => p.IsDescending ? q.OrderByDescending(i => i.SellingPrice)  : q.OrderBy(i => i.SellingPrice),
            "purchasePrice" => p.IsDescending ? q.OrderByDescending(i => i.PurchasePrice) : q.OrderBy(i => i.PurchasePrice),
            "createdAt"     => p.IsDescending ? q.OrderByDescending(i => i.CreatedAt)     : q.OrderBy(i => i.CreatedAt),
            _               => p.IsDescending ? q.OrderByDescending(i => i.Code)          : q.OrderBy(i => i.Code),
        };

        var total = await q.CountAsync();
        var rawItems = await q.Skip(p.Skip).Take(p.PerPage).ToListAsync();
        var data = rawItems.Select(i => ToDto(i)).ToList();

        return PaginatedResponse<ItemMasterDto>.Create(data, total, p.Page, p.PerPage);
    }

    public async Task<ItemMasterDto?> GetByIdAsync(Guid id)
    {
        var item = await _db.ItemMasters
            .Include(i => i.PreferredVendor)
            .FirstOrDefaultAsync(x => x.Id == id);
        return item is null ? null : ToDto(item);
    }

    public async Task<ItemMasterStatsDto> GetStatsAsync()
    {
        var totalAll = await _db.ItemMasters.CountAsync();
        var totalActive = await _db.ItemMasters.CountAsync(i => i.IsActive);
        var lowStockCount = await _db.ItemMasters.CountAsync(i => i.IsActive && i.Stock <= i.MinStock);
        var belowMinimumMarginCount = await ApplyBelowMinimumMarginFilter(_db.ItemMasters.AsQueryable()).CountAsync();
        return new ItemMasterStatsDto
        {
            TotalAll = totalAll,
            TotalActive = totalActive,
            LowStockCount = lowStockCount,
            BelowMinimumMarginCount = belowMinimumMarginCount,
        };
    }

    private static IQueryable<ItemMaster> ApplyBelowMinimumMarginFilter(IQueryable<ItemMaster> q) =>
        q.Where(i => i.PurchasePrice.HasValue && i.MarginMinimum.HasValue && i.MarginType.HasValue &&
            ((i.MarginType == MarginType.Percent && i.SellingPrice < i.PurchasePrice.Value * (1 + i.MarginMinimum.Value / 100))
             || (i.MarginType == MarginType.Nominal && i.SellingPrice < i.PurchasePrice.Value + i.MarginMinimum.Value)));

    public async Task<ItemMasterDto> CreateAsync(CreateItemMasterRequest req)
    {
        var code = await GenerateCodeAsync();
        Enum.TryParse<MarginType>(req.MarginType, true, out var marginType);
        var item = new ItemMaster
        {
            Code             = code,
            Name             = req.Name,
            Description      = req.Description,
            Category         = req.Category,
            Brand            = req.Brand,
            Uom              = req.Uom,
            Warehouse        = req.Warehouse,
            Stock            = req.Stock,
            MinStock         = req.MinStock,
            SellingPrice     = req.SellingPrice,
            PurchasePrice    = req.PurchasePrice,
            MarginType       = req.MarginType is null ? null : marginType,
            MarginDefault    = req.MarginDefault,
            MarginMinimum    = req.MarginMinimum,
            IsSellingPriceManual = req.IsSellingPriceManual,
            PreferredVendorId= req.PreferredVendorId,
            Model            = req.Model,
            LeadTimeDays     = req.LeadTimeDays,
            VendorItemCode   = req.VendorItemCode,
            ProcurementNotes = req.ProcurementNotes,
            ReorderPoint     = req.ReorderPoint,
            IsActive         = true,
        };

        if (!item.IsSellingPriceManual && ItemPricingCalculator.ComputeAutoSellingPrice(item) is { } autoPrice)
            item.SellingPrice = autoPrice;

        _db.ItemMasters.Add(item);
        await _db.SaveChangesAsync();

        await _db.Entry(item).Reference(i => i.PreferredVendor).LoadAsync();
        return ToDto(item);
    }

    public async Task<ItemMasterDto?> UpdateAsync(Guid id, UpdateItemMasterRequest req)
    {
        var item = await _db.ItemMasters
            .Include(i => i.PreferredVendor)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return null;

        Enum.TryParse<MarginType>(req.MarginType, true, out var marginType);

        item.Name             = req.Name;
        item.Description      = req.Description;
        item.Category         = req.Category;
        item.Brand            = req.Brand;
        item.Uom              = req.Uom;
        item.Warehouse        = req.Warehouse;
        item.Stock            = req.Stock;
        item.MinStock         = req.MinStock;
        item.SellingPrice     = req.SellingPrice;
        item.PurchasePrice    = req.PurchasePrice;
        item.MarginType       = req.MarginType is null ? null : marginType;
        item.MarginDefault    = req.MarginDefault;
        item.MarginMinimum    = req.MarginMinimum;
        item.IsSellingPriceManual = req.IsSellingPriceManual;
        item.PreferredVendorId= req.PreferredVendorId;
        item.Model            = req.Model;
        item.LeadTimeDays     = req.LeadTimeDays;
        item.VendorItemCode   = req.VendorItemCode;
        item.ProcurementNotes = req.ProcurementNotes;
        item.ReorderPoint     = req.ReorderPoint;
        item.UpdatedAt        = DateTimeOffset.UtcNow;

        // Auto mode always reflects the current cost/margin — re-derive harga_jual on every save,
        // not just when PurchasePrice itself changed (margin edits and manual→auto resets must also stick).
        if (!item.IsSellingPriceManual && ItemPricingCalculator.ComputeAutoSellingPrice(item) is { } autoPrice)
            item.SellingPrice = autoPrice;

        await _db.SaveChangesAsync();

        await _db.Entry(item).Reference(i => i.PreferredVendor).LoadAsync();
        return ToDto(item);
    }

    public async Task<BulkApplyMarginResultDto> BulkApplyAutoMarginAsync(BulkApplyMarginRequest req)
    {
        var q = _db.ItemMasters.Where(i => !i.IsSellingPriceManual);

        if (req.IsActive.HasValue)
            q = q.Where(i => i.IsActive == req.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var search = req.Search.ToLower();
            q = q.Where(i => i.Code.ToLower().Contains(search)
                         || i.Name.ToLower().Contains(search)
                         || (i.Category != null && i.Category.ToLower().Contains(search))
                         || (i.Brand != null && i.Brand.ToLower().Contains(search))
                         || (i.Warehouse != null && i.Warehouse.ToLower().Contains(search)));
        }

        var items = await q.ToListAsync();
        var updated = 0;
        var skipped = 0;

        foreach (var item in items)
        {
            if (ItemPricingCalculator.ComputeAutoSellingPrice(item) is { } autoPrice)
            {
                item.SellingPrice = autoPrice;
                item.UpdatedAt = DateTimeOffset.UtcNow;
                updated++;
            }
            else
            {
                skipped++;
            }
        }

        await _db.SaveChangesAsync();
        return new BulkApplyMarginResultDto { Updated = updated, Skipped = skipped };
    }

    public async Task<bool> SetStatusAsync(Guid id, bool isActive)
    {
        var item = await _db.ItemMasters.FindAsync(id);
        if (item is null) return false;
        item.IsActive  = isActive;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var item = await _db.ItemMasters.FindAsync(id);
        if (item is null) return false;
        item.IsDeleted = true;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<string> GenerateCodeAsync()
    {
        var count = await _db.ItemMasters.CountAsync() + 1;
        return $"ITM{count:D5}";
    }

    private static ItemMasterDto ToDto(ItemMaster item) => new()
    {
        Id                  = item.Id,
        Code                = item.Code,
        Name                = item.Name,
        Description         = item.Description,
        Category            = item.Category,
        Brand               = item.Brand,
        Uom                 = item.Uom,
        Warehouse           = item.Warehouse,
        Stock               = item.Stock,
        MinStock            = item.MinStock,
        SellingPrice        = item.SellingPrice,
        PurchasePrice       = item.PurchasePrice,
        LastPurchasePrice   = item.LastPurchasePrice,
        MarginType          = item.MarginType?.ToString().ToLowerInvariant(),
        MarginDefault       = item.MarginDefault,
        MarginMinimum       = item.MarginMinimum,
        IsSellingPriceManual= item.IsSellingPriceManual,
        PreferredVendorId   = item.PreferredVendorId,
        PreferredVendorName = item.PreferredVendor?.Name,
        Model               = item.Model,
        LeadTimeDays        = item.LeadTimeDays,
        VendorItemCode      = item.VendorItemCode,
        ProcurementNotes    = item.ProcurementNotes,
        IsInventoryItem     = item.IsInventoryItem,
        ReorderPoint        = item.ReorderPoint,
        IsActive            = item.IsActive,
        CreatedAt           = item.CreatedAt,
        UpdatedAt           = item.UpdatedAt,
    };
}
