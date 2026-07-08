using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public class PurchaseRequest : BaseEntity
{
    public string No { get; set; } = string.Empty;
    public Guid? SalesOrderId { get; set; }
    public Guid RequestedBy { get; set; }
    public DateOnly Date { get; set; }
    public PurchaseRequestStatus Status { get; set; } = PurchaseRequestStatus.Draft;
    public decimal Total { get; set; } = 0;
    public string? Notes { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }

    public SalesOrder? SalesOrder { get; set; }
    public User RequestedByUser { get; set; } = null!;
    public ICollection<PurchaseRequestItem> Items { get; set; } = [];
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = [];
}

public class PurchaseRequestItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PRId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal EstPrice { get; set; } = 0;
    public string? Notes { get; set; }
    public Guid? ItemMasterId { get; set; }
    public decimal OrderedQty { get; set; } = 0;

    public PurchaseRequest PurchaseRequest { get; set; } = null!;
    public ItemMaster? ItemMaster { get; set; }
}

public enum PurchaseRequestStatus
{
    Draft,
    Submitted,
    Approved,
    Rejected,
    Ordered,
    // PartiallyOrdered SENGAJA ditambahkan di AKHIR enum (bukan disisipkan di tengah) — meskipun
    // Status disimpan sebagai string di database (HasConversion<string>() di AppDbContext), urutan
    // deklarasi C# tetap dijaga stabil untuk konsistensi dan menghindari kebingungan di masa depan.
    PartiallyOrdered,
}
