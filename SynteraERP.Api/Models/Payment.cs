namespace SynteraERP.Api.Models;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvoiceId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public Guid? CashBankAccountId { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public Guid? RecordedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Invoice Invoice { get; set; } = null!;
    public Account? CashBankAccount { get; set; }
}

public enum PaymentMethod
{
    Transfer,
    Tunai,
    Giro,
    Cek
}
