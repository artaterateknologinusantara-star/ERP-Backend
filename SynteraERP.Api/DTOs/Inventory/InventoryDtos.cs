using SynteraERP.Api.Models;

namespace SynteraERP.Api.DTOs.Inventory;

public class StockTransactionDto
{
    public Guid Id { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public decimal StockBefore { get; set; }
    public decimal StockAfter { get; set; }
    public string? RefNo { get; set; }
    public string? Notes { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public class RecordStockInRequest
{
    public Guid ItemMasterId { get; set; }
    public decimal Qty { get; set; }
    public string? RefNo { get; set; }
    public Guid? RefId { get; set; }
    public string? Notes { get; set; }
    public StockTransactionSource Source { get; set; } = StockTransactionSource.Manual;
}

public class DeliveryOrderListDto
{
    public Guid Id { get; set; }
    public string No { get; set; } = string.Empty;
    public string? SalesOrderNo { get; set; }
    public string? CustomerName { get; set; }
    public DateOnly DeliveryDate { get; set; }
    public string? DeliveryAddress { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public class DeliveryOrderItemDto
{
    public Guid Id { get; set; }
    public Guid ItemMasterId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public decimal Qty { get; set; }
    public string Uom { get; set; } = string.Empty;
    public decimal QtyOrdered { get; set; }
    public decimal QtyPreviouslyOut { get; set; }
    public decimal CurrentStock { get; set; }
    public string? Notes { get; set; }
}

public class DeliveryOrderDetailDto : DeliveryOrderListDto
{
    public Guid? SalesOrderId { get; set; }
    public Guid? CustomerId { get; set; }
    public string? RecipientName { get; set; }
    public string? Notes { get; set; }
    public List<DeliveryOrderItemDto> Items { get; set; } = new();
}

public class CreateDeliveryOrderRequest
{
    public Guid? SalesOrderId { get; set; }
    public Guid? CustomerId { get; set; }
    public DateOnly DeliveryDate { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? RecipientName { get; set; }
    public string? Notes { get; set; }
    public List<CreateDeliveryOrderItemRequest> Items { get; set; } = new();
}

public class CreateDeliveryOrderItemRequest
{
    public Guid ItemMasterId { get; set; }
    public decimal Qty { get; set; }
    public string Uom { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int SortOrder { get; set; } = 0;
}

public class InventoryStatsDto
{
    public int TotalItems { get; set; }
    public int ActiveItems { get; set; }
    public int LowStockItems { get; set; }
    public int OutOfStockItems { get; set; }
    public decimal TotalStockValue { get; set; }
    public int PendingDOs { get; set; }
    public int ActiveDOs { get; set; }
}

public class LowStockItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public decimal Stock { get; set; }
    public decimal MinStock { get; set; }
    public string Uom { get; set; } = string.Empty;
    public decimal Shortage { get; set; }
}
