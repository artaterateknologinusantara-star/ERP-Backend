using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public class SupplierInvoice : BaseEntity
{
    public string No { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid PurchaseOrderId { get; set; }
    public Guid SupplierId { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Subtotal { get; set; } = 0;
    public decimal PPNMasukan { get; set; } = 0;
    public decimal Total { get; set; } = 0;
    public string? NomorFakturPajak { get; set; }
    public SupplierInvoiceStatus Status { get; set; } = SupplierInvoiceStatus.Draft;
    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }

    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
    public ICollection<SupplierInvoiceItem> Items { get; set; } = [];
    public ICollection<SupplierInvoicePayment> Payments { get; set; } = [];
}

public enum SupplierInvoiceStatus
{
    Draft,
    Approved,
    PartiallyPaid,
    Paid,
    Cancelled,
}
