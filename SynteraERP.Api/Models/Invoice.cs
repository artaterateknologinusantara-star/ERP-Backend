using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public class Invoice : BaseEntity
{
    public string No { get; set; } = string.Empty;
    public Guid? SalesOrderId { get; set; }
    public Guid CustomerId { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal Paid { get; set; } = 0;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public string? Notes { get; set; }

    public decimal Balance => Amount - Paid;

    public SalesOrder? SalesOrder { get; set; }
    public Customer Customer { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = [];
}

public enum InvoiceStatus
{
    Draft,
    Sent,
    PartialPaid,
    Paid,
    Overdue
}
