namespace SynteraERP.Api.Models;

public class SalesOrderPayment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;

    public DateOnly PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public Guid? CashBankAccountId { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Account? CashBankAccount { get; set; }
    public ICollection<DownPaymentApplication> Applications { get; set; } = [];
}
