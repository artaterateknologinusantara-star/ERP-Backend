namespace SynteraERP.Api.DTOs.SalesOrder;

public class SalesOrderItemDto
{
    public Guid? ItemMasterId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public decimal Qty { get; set; }
    public string Uom { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; } = 0;
    public string? Notes { get; set; }
    public int SortOrder { get; set; } = 0;
}

public class CreateSalesOrderRequest
{
    public Guid? QuotationId { get; set; }
    public Guid CustomerId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public Guid SalesId { get; set; }
    public DateOnly? ExpectedDate { get; set; }
    public string? ShipTo { get; set; }
    public string? Terms { get; set; }
    public string? RefQuotation { get; set; }
    public string? Notes { get; set; }
    public List<SalesOrderItemDto> Items { get; set; } = new();
}

public class SalesOrderItemResponse
{
    public Guid Id { get; set; }
    public Guid? ItemMasterId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public decimal Qty { get; set; }
    public string Uom { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal Amount { get; set; }
    public decimal QtyShipped { get; set; }
    public decimal QtyNotShipped { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
}

public class SalesOrderDetailResponse
{
    public Guid Id { get; set; }
    public string No { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerAddress { get; set; }
    public string? CustomerNpwp { get; set; }
    public string? CustomerContactPerson { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public Guid SalesId { get; set; }
    public string SalesName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public DateOnly? ExpectedDate { get; set; }
    public string? ShipTo { get; set; }
    public string? Terms { get; set; }
    public string? RefQuotation { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public List<SalesOrderItemResponse> Items { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
}

public class SalesOrderListResponse
{
    public Guid Id { get; set; }
    public string No { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public DateOnly? ExpectedDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal GrandTotal { get; set; }
    public string? RefQuotation { get; set; }
}

public class SalesOrderStatsResponse
{
    public int Total { get; set; }
    public int Open { get; set; }
    public int Delivered { get; set; }
    public int CompletedThisMonth { get; set; }
    public decimal TotalValue { get; set; }
}
