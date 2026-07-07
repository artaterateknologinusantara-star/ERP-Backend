using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public enum StockTransactionType
{
    StockIn,
    StockOut,
    Return,
    Adjustment
}

public enum StockTransactionSource
{
    PurchaseOrder,
    DeliveryOrder,
    Manual,
    Return
}

public class StockTransaction : BaseEntity
{
    public Guid ItemMasterId { get; set; }
    public ItemMaster ItemMaster { get; set; } = null!;

    public StockTransactionType Type { get; set; }
    public StockTransactionSource Source { get; set; }

    public decimal Qty { get; set; }
    public decimal StockBefore { get; set; }
    public decimal StockAfter { get; set; }

    public string? RefNo { get; set; }
    public Guid? RefId { get; set; }
    public string? Notes { get; set; }

    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
}
