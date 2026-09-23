using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.CatalogLink;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

/// <summary>Layar admin "Item Belum Terhubung" — daftar baris Quotation/Sales Order lama yang
/// belum punya ItemMasterId (dari sebelum field ini ada, atau sebelum fallback tebak-nama SO→
/// Item Master dihapus — lihat InventoryService.MatchSoItemToItemMasterAsync). SENGAJA tidak ada
/// auto-backfill fuzzy apa pun: admin memilih link yang benar satu-satu, kapan pun mereka mau.</summary>
public class CatalogLinkService : ICatalogLinkService
{
    private readonly AppDbContext _db;
    private readonly IQuotationService _quotationService;
    private readonly IInventoryService _inventoryService;

    public CatalogLinkService(AppDbContext db, IQuotationService quotationService, IInventoryService inventoryService)
    {
        _db = db;
        _quotationService = quotationService;
        _inventoryService = inventoryService;
    }

    public async Task<List<UnlinkedCatalogItemDto>> GetUnlinkedItemsAsync()
    {
        var quotationRows = await _db.QuotationItems
            .AsNoTracking()
            .Where(i => i.ItemMasterId == null
                && !i.Group.Tab.Quotation.IsDeleted)
            .Select(i => new UnlinkedCatalogItemDto
            {
                ItemRowId = i.Id,
                SourceType = "Quotation",
                SourceNo = i.Group.Tab.Quotation.No,
                Description = i.Equipment,
                Sku = i.ItemNo,
                Qty = i.Qty,
                Uom = i.Unit,
            })
            .ToListAsync();

        var soRows = await _db.SalesOrderItems
            .AsNoTracking()
            .Where(i => i.ItemMasterId == null && !i.SalesOrder.IsDeleted)
            .Select(i => new UnlinkedCatalogItemDto
            {
                ItemRowId = i.Id,
                SourceType = "SalesOrder",
                SourceNo = i.SalesOrder.No,
                Description = i.Description,
                Sku = i.Sku,
                Qty = i.Qty,
                Uom = i.Uom,
            })
            .ToListAsync();

        return quotationRows.Concat(soRows)
            .OrderBy(x => x.SourceType).ThenBy(x => x.SourceNo)
            .ToList();
    }

    public async Task LinkAsync(string sourceType, Guid itemRowId, Guid itemMasterId)
    {
        switch (sourceType)
        {
            case "Quotation":
                await _quotationService.LinkItemMasterAsync(itemRowId, itemMasterId);
                break;
            case "SalesOrder":
                await _inventoryService.LinkSoItemToItemMasterAsync(itemRowId, itemMasterId);
                break;
            default:
                throw new InvalidOperationException($"sourceType '{sourceType}' tidak dikenal.");
        }
    }
}
