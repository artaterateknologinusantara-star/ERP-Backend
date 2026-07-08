using SynteraERP.Api.DTOs.Common;

namespace SynteraERP.Api.DTOs.SupplierInvoice;

public class SupplierInvoiceItemDto
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public decimal Price { get; set; }
    public decimal Amount { get; set; }
}

public class SupplierInvoiceDto
{
    public Guid Id { get; set; }
    public string No { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid PurchaseOrderId { get; set; }
    public string PurchaseOrderNo { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal PPNMasukan { get; set; }
    public string? NomorFakturPajak { get; set; }
    public decimal Total { get; set; }
    public decimal Paid { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? ApprovedAt { get; set; }
    public List<SupplierInvoiceItemDto> Items { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
}

public class SupplierInvoiceListDto
{
    public Guid Id { get; set; }
    public string No { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string PurchaseOrderNo { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Total { get; set; }
    public decimal Paid { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class SupplierInvoiceQueryParams : PaginationParams
{
    public string? Status { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
}

public class CreateSupplierInvoiceItemRequest
{
    public Guid PurchaseOrderItemId { get; set; }
    public decimal Qty { get; set; }
}

public class CreateSupplierInvoiceRequest
{
    public Guid PurchaseOrderId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal PPNMasukan { get; set; }
    public string? NomorFakturPajak { get; set; }
    public List<CreateSupplierInvoiceItemRequest> Items { get; set; } = [];
}
