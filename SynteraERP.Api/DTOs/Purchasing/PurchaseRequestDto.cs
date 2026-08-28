using SynteraERP.Api.DTOs.Common;

namespace SynteraERP.Api.DTOs.Purchasing;

public class PurchaseRequestQueryParams : PaginationParams
{
    public string? Status { get; set; }
}

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
    public int ItemCount { get; set; }
}

public class PurchaseRequestDto : PurchaseRequestListDto
{
    public Guid RequestedBy { get; set; }
    public Guid? SalesOrderId { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int ItemsNeedingPriceVerification { get; set; }
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
    public decimal OrderedQty { get; set; }
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

public class PurchaseRequestStatsDto
{
    public int Total { get; set; }
    public int Draft { get; set; }
    public int Submitted { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public decimal TotalValue { get; set; }
}
