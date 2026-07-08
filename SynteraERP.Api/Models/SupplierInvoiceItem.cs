namespace SynteraERP.Api.Models;

public class SupplierInvoiceItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SupplierInvoiceId { get; set; }
    public Guid PurchaseOrderItemId { get; set; }
    public decimal Qty { get; set; }
    public decimal Price { get; set; }

    public decimal Amount => Qty * Price;

    public SupplierInvoice SupplierInvoice { get; set; } = null!;
    public PurchaseOrderItem PurchaseOrderItem { get; set; } = null!;
}
