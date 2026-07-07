using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public class SalesOrderItem : BaseEntity
{
    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;
    public Guid? ItemMasterId { get; set; }
    public ItemMaster? ItemMaster { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public decimal Qty { get; set; }
    public string Uom { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; } = 0;
    public decimal Amount { get; set; }
    public decimal QtyShipped { get; set; } = 0;
    public decimal QtyNotShipped => Qty - QtyShipped;
    public string? Notes { get; set; }
    public int SortOrder { get; set; } = 0;
}
