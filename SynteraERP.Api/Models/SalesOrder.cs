using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public class SalesOrder : BaseEntity
{
    public string No { get; set; } = string.Empty;
    public Guid? QuotationId { get; set; }
    public Guid CustomerId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public DateOnly? DeliveryDate { get; set; }
    public Guid SalesId { get; set; }
    public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Draft;
    public decimal Total { get; set; } = 0;
    public string? Notes { get; set; }

    public Quotation? Quotation { get; set; }
    public Customer Customer { get; set; } = null!;
    public User Sales { get; set; } = null!;
    public ICollection<Invoice> Invoices { get; set; } = [];
    public ICollection<PurchaseRequest> PurchaseRequests { get; set; } = [];
}

public enum SalesOrderStatus
{
    Draft,
    Open,
    Delivered,
    Completed,
    Cancelled
}
