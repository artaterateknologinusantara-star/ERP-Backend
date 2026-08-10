namespace SynteraERP.Api.DTOs.SalesOrderPayment;

public class SalesOrderPaymentDto
{
    public Guid Id { get; set; }
    public Guid SalesOrderId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public decimal AmountApplied { get; set; }
    public decimal Remaining { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class RecordDownPaymentRequest
{
    public DateOnly? PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "Transfer";
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public class ApplyDownPaymentRequest
{
    public Guid SalesOrderPaymentId { get; set; }
    public decimal AmountToApply { get; set; }
}
