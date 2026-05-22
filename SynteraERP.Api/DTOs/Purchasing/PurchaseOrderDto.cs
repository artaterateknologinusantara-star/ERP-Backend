namespace SynteraERP.Api.DTOs.Purchasing;

public class PurchaseOrderListDto
{
    public Guid Id { get; set; }
    public string No { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? PurchaseRequestNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateOnly? DeliveryDate { get; set; }
}

public class PurchaseOrderDto : PurchaseOrderListDto
{
    public Guid SupplierId { get; set; }
    public Guid? PurchaseRequestId { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<PurchaseOrderItemDto> Items { get; set; } = [];
}

public class PurchaseOrderItemDto
{
    public Guid Id { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Total { get; set; }
    public decimal ReceivedQty { get; set; }
}

public class CreatePurchaseOrderRequest
{
    public Guid SupplierId { get; set; }
    public Guid? PurchaseRequestId { get; set; }
    public DateOnly Date { get; set; }
    public DateOnly? DeliveryDate { get; set; }
    public string? Notes { get; set; }
    public List<CreatePOItemRequest> Items { get; set; } = [];
}

public class CreatePOItemRequest
{
    public string ItemName { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class UpdatePOStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class ReceiveGoodsRequest
{
    public List<ReceiveGoodsItemRequest> Items { get; set; } = [];
}

public class ReceiveGoodsItemRequest
{
    public Guid ItemId { get; set; }
    public decimal ReceivedQty { get; set; }
}
