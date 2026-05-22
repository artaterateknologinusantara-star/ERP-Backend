namespace SynteraERP.Api.DTOs.Invoice;

public class InvoiceListDto
{
    public Guid Id { get; set; }
    public string No { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public DateOnly DueDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? SalesOrderNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Paid { get; set; }
    public decimal Balance { get; set; }
}

public class InvoiceDto : InvoiceListDto
{
    public Guid CustomerId { get; set; }
    public Guid? SalesOrderId { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<PaymentDto> Payments { get; set; } = [];
}

public class PaymentDto
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
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
}

public class RecordPaymentRequest
{
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "Transfer";
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}
