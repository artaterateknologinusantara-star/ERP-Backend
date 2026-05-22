namespace SynteraERP.Api.DTOs.Purchasing;

public class PurchaseRequestListDto
{
    public Guid Id { get; set; }
    public string No { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string RequestedByName { get; set; } = string.Empty;
    public string? SalesOrderNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string? Notes { get; set; }
}

public class PurchaseRequestDto : PurchaseRequestListDto
{
    public Guid RequestedBy { get; set; }
    public Guid? SalesOrderId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<PurchaseRequestItemDto> Items { get; set; } = [];
}

public class PurchaseRequestItemDto
{
    public Guid Id { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal EstPrice { get; set; }
    public string? Notes { get; set; }
}

public class CreatePurchaseRequestRequest
{
    public Guid RequestedBy { get; set; }
    public Guid? SalesOrderId { get; set; }
    public DateOnly Date { get; set; }
    public string? Notes { get; set; }
    public List<CreatePRItemRequest> Items { get; set; } = [];
}

public class CreatePRItemRequest
{
    public string ItemName { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal EstPrice { get; set; }
    public string? Notes { get; set; }
}

public class UpdatePRStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
