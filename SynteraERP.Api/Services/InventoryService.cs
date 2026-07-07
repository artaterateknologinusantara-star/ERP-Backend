using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Inventory;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class InventoryService : IInventoryService
{
    private readonly AppDbContext _db;

    public InventoryService(AppDbContext db) => _db = db;

    // ─── Stats ────────────────────────────────────────────────────────────────

    public async Task<InventoryStatsDto> GetStatsAsync()
    {
        var items = await _db.ItemMasters
            .Where(x => x.IsActive && !x.IsDeleted)
            .ToListAsync();

        var dos = await _db.DeliveryOrders
            .Where(x => !x.IsDeleted)
            .ToListAsync();

        return new InventoryStatsDto
        {
            TotalItems = items.Count,
            ActiveItems = items.Count(x => x.IsActive),
            LowStockItems = items.Count(x => x.Stock <= x.MinStock && x.Stock > 0),
            OutOfStockItems = items.Count(x => x.Stock == 0),
            // Nilai stok dihitung dari PurchasePrice (COGS) jika tersedia, fallback ke SellingPrice
            TotalStockValue = items.Sum(x => x.Stock * (x.PurchasePrice ?? x.SellingPrice)),
            PendingDOs = dos.Count(x => x.Status == DeliveryOrderStatus.Draft),
            ActiveDOs = dos.Count(x => x.Status == DeliveryOrderStatus.Confirmed),
        };
    }

    public async Task<List<LowStockItemDto>> GetLowStockItemsAsync()
    {
        var items = await _db.ItemMasters
            .Where(x => x.IsActive && !x.IsDeleted && x.Stock <= x.MinStock)
            .ToListAsync();

        return items
            .OrderBy(x => x.MinStock > 0 ? (double)(x.Stock / x.MinStock) : 0)
            .Select(x => new LowStockItemDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Category = x.Category,
                Stock = x.Stock,
                MinStock = x.MinStock,
                Uom = x.Uom,
                Shortage = x.MinStock - x.Stock,
            })
            .ToList();
    }

    // ─── Stock transactions ───────────────────────────────────────────────────

    public async Task<List<StockTransactionDto>> GetStockHistoryAsync(
        Guid? itemMasterId, int page, int perPage)
    {
        var q = _db.StockTransactions
            .Include(x => x.ItemMaster)
            .Include(x => x.CreatedByUser)
            .AsQueryable();

        if (itemMasterId.HasValue)
            q = q.Where(x => x.ItemMasterId == itemMasterId.Value);

        return await q
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(x => ToStockTxDto(x))
            .ToListAsync();
    }

    public async Task<StockTransactionDto> RecordStockInAsync(
        RecordStockInRequest request, Guid userId)
    {
        var item = await _db.ItemMasters.FindAsync(request.ItemMasterId)
            ?? throw new InvalidOperationException("Item tidak ditemukan.");

        if (!item.IsActive)
            throw new InvalidOperationException("Item tidak aktif.");

        var stockBefore = item.Stock;
        item.Stock += request.Qty;
        item.UpdatedAt = DateTimeOffset.UtcNow;

        var tx = new StockTransaction
        {
            ItemMasterId = item.Id,
            Type = StockTransactionType.StockIn,
            Source = request.Source,
            Qty = request.Qty,
            StockBefore = stockBefore,
            StockAfter = item.Stock,
            RefNo = request.RefNo,
            RefId = request.RefId,
            Notes = request.Notes,
            CreatedByUserId = userId,
        };

        _db.StockTransactions.Add(tx);
        await _db.SaveChangesAsync();

        await _db.Entry(tx).Reference(x => x.ItemMaster).LoadAsync();
        await _db.Entry(tx).Reference(x => x.CreatedByUser).LoadAsync();

        return ToStockTxDto(tx);
    }

    // ─── Delivery orders ─────────────────────────────────────────────────────

    public async Task<List<DeliveryOrderListDto>> GetDeliveryOrdersAsync(
        int page, int perPage, string? search, string? status)
    {
        var q = _db.DeliveryOrders
            .Include(x => x.Customer)
            .Include(x => x.SalesOrder)
            .Include(x => x.CreatedByUser)
            .Include(x => x.Items)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            q = q.Where(x =>
                x.No.ToLower().Contains(s) ||
                (x.Customer != null && x.Customer.Name.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<DeliveryOrderStatus>(status, true, out var parsedStatus))
        {
            q = q.Where(x => x.Status == parsedStatus);
        }

        return await q
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(x => ToListDto(x))
            .ToListAsync();
    }

    public async Task<DeliveryOrderDetailDto?> GetDeliveryOrderByIdAsync(Guid id)
    {
        var do_ = await _db.DeliveryOrders
            .Include(x => x.Customer)
            .Include(x => x.SalesOrder)
            .Include(x => x.CreatedByUser)
            .Include(x => x.Items)
                .ThenInclude(i => i.ItemMaster)
            .FirstOrDefaultAsync(x => x.Id == id);

        return do_ is null ? null : ToDetailDto(do_);
    }

    public async Task<DeliveryOrderDetailDto> CreateDeliveryOrderAsync(
        CreateDeliveryOrderRequest request, Guid userId)
    {
        // Validate items
        foreach (var req in request.Items)
        {
            var item = await _db.ItemMasters.FindAsync(req.ItemMasterId)
                ?? throw new InvalidOperationException($"Item dengan ID {req.ItemMasterId} tidak ditemukan.");

            if (!item.IsActive)
                throw new InvalidOperationException($"Item '{item.Name}' tidak aktif.");

            if (item.Stock < req.Qty)
                throw new InvalidOperationException(
                    $"Stok {item.Name} tidak cukup. Stok tersedia: {item.Stock} {item.Uom}, diminta: {req.Qty}");
        }

        var no = await NextDONumberAsync();

        var doEntity = new DeliveryOrder
        {
            No = no,
            SalesOrderId = request.SalesOrderId,
            CustomerId = request.CustomerId,
            DeliveryDate = request.DeliveryDate,
            DeliveryAddress = request.DeliveryAddress,
            RecipientName = request.RecipientName,
            Notes = request.Notes,
            Status = DeliveryOrderStatus.Draft,
            CreatedByUserId = userId,
        };

        foreach (var req in request.Items)
        {
            var item = await _db.ItemMasters.FindAsync(req.ItemMasterId)!;
            doEntity.Items.Add(new DeliveryOrderItem
            {
                ItemMasterId = req.ItemMasterId,
                ItemName = item!.Name,
                Sku = item.Code,
                Qty = req.Qty,
                Uom = req.Uom,
                QtyOrdered = req.Qty,
                QtyPreviouslyOut = 0,
                Notes = req.Notes,
                SortOrder = req.SortOrder,
            });
        }

        _db.DeliveryOrders.Add(doEntity);
        await _db.SaveChangesAsync();

        return (await GetDeliveryOrderByIdAsync(doEntity.Id))!;
    }

    public async Task<DeliveryOrderDetailDto> ConfirmDeliveryOrderAsync(Guid id, Guid userId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var doEntity = await _db.DeliveryOrders
            .Include(x => x.Items)
                .ThenInclude(i => i.ItemMaster)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new InvalidOperationException("Delivery Order tidak ditemukan.");

        if (doEntity.Status != DeliveryOrderStatus.Draft)
            throw new InvalidOperationException(
                $"Hanya DO berstatus Draft yang bisa dikonfirmasi. Status saat ini: {doEntity.Status}");

        // Re-validate stock (may have changed since DO was created)
        foreach (var doItem in doEntity.Items)
        {
            if (doItem.ItemMaster.Stock < doItem.Qty)
                throw new InvalidOperationException(
                    $"Stok {doItem.ItemMaster.Name} tidak cukup. " +
                    $"Stok tersedia: {doItem.ItemMaster.Stock} {doItem.ItemMaster.Uom}, " +
                    $"diminta: {doItem.Qty}");
        }

        foreach (var doItem in doEntity.Items)
        {
            var stockBefore = doItem.ItemMaster.Stock;
            doItem.ItemMaster.Stock -= doItem.Qty;
            doItem.ItemMaster.UpdatedAt = DateTimeOffset.UtcNow;

            _db.StockTransactions.Add(new StockTransaction
            {
                ItemMasterId = doItem.ItemMaster.Id,
                Type = StockTransactionType.StockOut,
                Source = StockTransactionSource.DeliveryOrder,
                Qty = -doItem.Qty,
                StockBefore = stockBefore,
                StockAfter = doItem.ItemMaster.Stock,
                RefNo = doEntity.No,
                RefId = doEntity.Id,
                Notes = $"Delivery Order {doEntity.No}",
                CreatedByUserId = userId,
            });
        }

        doEntity.Status = DeliveryOrderStatus.Confirmed;
        doEntity.UpdatedAt = DateTimeOffset.UtcNow;

        if (doEntity.SalesOrderId.HasValue)
        {
            var so = await _db.SalesOrders
                .FirstOrDefaultAsync(x => x.Id == doEntity.SalesOrderId.Value);
            if (so != null && so.Status == SalesOrderStatus.Open)
            {
                so.Status = SalesOrderStatus.Delivered;
                so.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return (await GetDeliveryOrderByIdAsync(id))!;
    }

    public async Task<DeliveryOrderDetailDto> MarkDeliveredAsync(Guid id)
    {
        var doEntity = await _db.DeliveryOrders.FindAsync(id)
            ?? throw new InvalidOperationException("Delivery Order tidak ditemukan.");

        if (doEntity.Status != DeliveryOrderStatus.Confirmed)
            throw new InvalidOperationException(
                $"Hanya DO berstatus Confirmed yang bisa ditandai Delivered. Status saat ini: {doEntity.Status}");

        doEntity.Status = DeliveryOrderStatus.Delivered;
        doEntity.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

        return (await GetDeliveryOrderByIdAsync(id))!;
    }

    public async Task DeleteDeliveryOrderAsync(Guid id)
    {
        var doEntity = await _db.DeliveryOrders.FindAsync(id)
            ?? throw new InvalidOperationException("Delivery Order tidak ditemukan.");

        if (doEntity.Status != DeliveryOrderStatus.Draft)
            throw new InvalidOperationException(
                "Hanya DO berstatus Draft yang bisa dihapus.");

        doEntity.IsDeleted = true;
        doEntity.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
    }

    // ─── Create DO from Sales Order ──────────────────────────────────────────

    public async Task<DeliveryOrderDetailDto> CreateDOFromSOAsync(Guid soId, Guid userId)
    {
        // 1. Load SO dengan items + customer
        var so = await _db.SalesOrders
            .Include(x => x.Customer)
            .Include(x => x.Items.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(x => x.Id == soId && !x.IsDeleted)
            ?? throw new InvalidOperationException("Sales Order tidak ditemukan.");

        // 2. Validasi status
        if (so.Status != SalesOrderStatus.Open && so.Status != SalesOrderStatus.Delivered)
            throw new InvalidOperationException(
                "Sales Order harus berstatus Open atau Delivered.");

        // 3. Validasi ada items
        if (!so.Items.Any())
            throw new InvalidOperationException(
                "Sales Order tidak memiliki item. Tambah items ke SO terlebih dahulu.");

        // 4. Match SO items ke ItemMaster
        var doItems = new List<CreateDeliveryOrderItemRequest>();

        foreach (var soItem in so.Items.OrderBy(x => x.SortOrder))
        {
            ItemMaster? master = null;

            // Match by SKU (Code) terlebih dahulu
            if (!string.IsNullOrEmpty(soItem.Sku))
                master = await _db.ItemMasters
                    .FirstOrDefaultAsync(x => x.Code == soItem.Sku && x.IsActive && !x.IsDeleted);

            // Fallback: match by kata pertama nama item
            if (master == null && !string.IsNullOrEmpty(soItem.Description))
            {
                var firstWord = soItem.Description
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .First()
                    .ToLower();

                master = await _db.ItemMasters
                    .FirstOrDefaultAsync(x =>
                        x.Name.ToLower().Contains(firstWord) && x.IsActive && !x.IsDeleted);
            }

            // Hanya tambah ke DO jika ada di inventory
            if (master != null)
            {
                doItems.Add(new CreateDeliveryOrderItemRequest
                {
                    ItemMasterId = master.Id,
                    Qty          = soItem.Qty,
                    Uom          = !string.IsNullOrEmpty(soItem.Uom) ? soItem.Uom : master.Uom,
                    Notes        = soItem.Notes,
                    SortOrder    = soItem.SortOrder,
                });
            }
        }

        if (!doItems.Any())
            throw new InvalidOperationException(
                "Tidak ada item SO yang cocok di Item Master. " +
                "Pastikan item terdaftar di Item Master dengan SKU atau nama yang sesuai.");

        // 5. Buat DO via method yang sudah ada
        var request = new CreateDeliveryOrderRequest
        {
            SalesOrderId    = soId,
            CustomerId      = so.CustomerId,
            DeliveryDate    = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            DeliveryAddress = so.ShipTo ?? so.Customer?.Address,
            Notes           = $"DO dari SO {so.No}",
            Items           = doItems,
        };

        return await CreateDeliveryOrderAsync(request, userId);
    }

    // ─── Numbering ────────────────────────────────────────────────────────────

    private async Task<string> NextDONumberAsync()
    {
        var config = await _db.NumberingConfigs
            .FirstOrDefaultAsync(n => n.DocType == "DELIVERY_ORDER")
            ?? throw new InvalidOperationException("NumberingConfig for DELIVERY_ORDER not found. Please seed the numbering config.");

        var no = config.GenerateNext();
        await _db.SaveChangesAsync();
        return no;
    }

    // ─── Mapping helpers ──────────────────────────────────────────────────────

    private static StockTransactionDto ToStockTxDto(StockTransaction x) => new()
    {
        Id = x.Id,
        ItemCode = x.ItemMaster?.Code ?? string.Empty,
        ItemName = x.ItemMaster?.Name ?? string.Empty,
        Type = x.Type.ToString(),
        Source = x.Source.ToString(),
        Qty = x.Qty,
        StockBefore = x.StockBefore,
        StockAfter = x.StockAfter,
        RefNo = x.RefNo,
        Notes = x.Notes,
        CreatedByName = x.CreatedByUser?.Name ?? string.Empty,
        CreatedAt = x.CreatedAt,
    };

    private static DeliveryOrderListDto ToListDto(DeliveryOrder x) => new()
    {
        Id = x.Id,
        No = x.No,
        SalesOrderNo = x.SalesOrder?.No,
        CustomerName = x.Customer?.Name,
        DeliveryDate = x.DeliveryDate,
        DeliveryAddress = x.DeliveryAddress,
        Status = x.Status.ToString(),
        ItemCount = x.Items.Count,
        CreatedByName = x.CreatedByUser?.Name ?? string.Empty,
        CreatedAt = x.CreatedAt,
    };

    private static DeliveryOrderDetailDto ToDetailDto(DeliveryOrder x)
    {
        var dto = new DeliveryOrderDetailDto
        {
            Id = x.Id,
            No = x.No,
            SalesOrderId = x.SalesOrderId,
            SalesOrderNo = x.SalesOrder?.No,
            CustomerId = x.CustomerId,
            CustomerName = x.Customer?.Name,
            DeliveryDate = x.DeliveryDate,
            DeliveryAddress = x.DeliveryAddress,
            RecipientName = x.RecipientName,
            Status = x.Status.ToString(),
            Notes = x.Notes,
            ItemCount = x.Items.Count,
            CreatedByName = x.CreatedByUser?.Name ?? string.Empty,
            CreatedAt = x.CreatedAt,
            Items = x.Items
                .OrderBy(i => i.SortOrder)
                .Select(i => new DeliveryOrderItemDto
                {
                    Id = i.Id,
                    ItemMasterId = i.ItemMasterId,
                    ItemName = i.ItemName,
                    Sku = i.Sku,
                    Qty = i.Qty,
                    Uom = i.Uom,
                    QtyOrdered = i.QtyOrdered,
                    QtyPreviouslyOut = i.QtyPreviouslyOut,
                    CurrentStock = i.ItemMaster?.Stock ?? 0,
                    Notes = i.Notes,
                })
                .ToList(),
        };
        return dto;
    }
}
