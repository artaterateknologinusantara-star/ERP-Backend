using SynteraERP.Api.DTOs.Common;

namespace SynteraERP.Api.DTOs.Invoice;

public class InvoiceQueryParams : PaginationParams
{
    public string? Status { get; set; }
}

public class InvoiceListDto
{
    public Guid Id { get; set; }
    public string No { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? SalesOrderNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Paid { get; set; }
    public decimal Balance { get; set; }
    public int AgingDays { get; set; }
}

public class InvoiceDto : InvoiceListDto
{
    public Guid CustomerId { get; set; }
    public Guid? SalesOrderId { get; set; }
    public string? Notes { get; set; }
    public string? Terms { get; set; }
    public string? NomorFakturPajak { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<PaymentDto> Payments { get; set; } = [];
    public List<InvoiceItemResponse> Items { get; set; } = [];
}

public class InvoiceItemResponse
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public decimal Qty { get; set; }
    public string Uom { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public int SortOrder { get; set; }
}

public class PaymentDto
{
    public Guid Id { get; set; }
    public DateOnly PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public class CreateInvoiceRequest
{
    public Guid CustomerId { get; set; }
    public Guid? SalesOrderId { get; set; }
    public DateOnly Date { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public string? Terms { get; set; }
    public string? NomorFakturPajak { get; set; }
}

public class RecordPaymentRequest
{
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "Transfer";
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public class InvoiceStatsResponse
{
    public int Total { get; set; }
    public int Outstanding { get; set; }
    public int Overdue { get; set; }
    public decimal TotalCollected { get; set; }
}
