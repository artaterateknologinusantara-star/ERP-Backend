using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public enum DeliveryOrderStatus
{
    Draft,
    Confirmed,
    Delivered,
    Returned,
    Cancelled
}

public class DeliveryOrder : BaseEntity
{
    public string No { get; set; } = string.Empty;

    public Guid? SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public DateOnly DeliveryDate { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? RecipientName { get; set; }
    public string? Notes { get; set; }

    public DeliveryOrderStatus Status { get; set; } = DeliveryOrderStatus.Draft;

    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public ICollection<DeliveryOrderItem> Items { get; set; } = new List<DeliveryOrderItem>();
}

public class DeliveryOrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeliveryOrderId { get; set; }
    public DeliveryOrder DeliveryOrder { get; set; } = null!;

    public Guid ItemMasterId { get; set; }
    public ItemMaster ItemMaster { get; set; } = null!;

    public string ItemName { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public decimal Qty { get; set; }
    public string Uom { get; set; } = string.Empty;

    public decimal QtyOrdered { get; set; }
    public decimal QtyPreviouslyOut { get; set; } = 0;

    public string? Notes { get; set; }
    public int SortOrder { get; set; } = 0;
}
