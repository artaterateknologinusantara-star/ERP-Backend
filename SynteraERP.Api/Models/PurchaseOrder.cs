using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public class PurchaseOrder : BaseEntity
{
    public string No { get; set; } = string.Empty;
    public Guid? PurchaseRequestId { get; set; }
    public Guid SupplierId { get; set; }
    public DateOnly Date { get; set; }
    public DateOnly? DeliveryDate { get; set; }
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public decimal Total { get; set; } = 0;
    public string? Notes { get; set; }

    public PurchaseRequest? PurchaseRequest { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public ICollection<PurchaseOrderItem> Items { get; set; } = [];
    public ICollection<POPayment> Payments { get; set; } = new List<POPayment>();
}

public class POPayment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public DateOnly PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public Guid? CashBankAccountId { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Account? CashBankAccount { get; set; }
}

public class PurchaseOrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid POId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal ReceivedQty { get; set; } = 0;
    public decimal InvoicedQty { get; set; } = 0;
    public Guid? ItemMasterId { get; set; }

    public decimal Total => Qty * Price;

    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public ItemMaster? ItemMaster { get; set; }
}

public enum PurchaseOrderStatus
{
    Draft,
    Ordered,
    PartialReceive,
    Completed,
    Cancelled
}
