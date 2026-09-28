using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

// "Permintaan RAB" yang dikirim maincon ke 1 vendor untuk 1 QuotationGroup/scope. Baris item
// (Name/Spesifikasi/Volume/Unit) di-draft oleh maincon lewat Lines — vendor hanya mengisi
// UnitPrice lewat VendorRabSubmission, tidak pernah menambah/mengubah baris sendiri. Data di
// sini TIDAK PERNAH otomatis menjadi QuotationWorkItem/WorkDetail resmi (yang ikut ke-generate
// PDF) — itu hanya terjadi saat maincon approve sebuah VendorRabSubmission secara eksplisit.
// 1 Group boleh punya banyak request ke vendor berbeda (unique per Group+Supplier, bukan per
// Group saja) — lihat index di AppDbContext.
public class VendorRabRequest : BaseEntity
{
    public Guid QuotationGroupId { get; set; }
    public Guid SupplierId { get; set; }

    // Jadi nama QuotationWorkItem begitu salah satu submission di-approve — netral/tidak boleh
    // menyebut nama vendor (mis. "Pekerjaan Sipil - Lantai 2", bukan "Pekerjaan PT ABC").
    public string Name { get; set; } = string.Empty;

    public VendorRabRequestStatus Status { get; set; } = VendorRabRequestStatus.Draft;
    public DateTimeOffset? SentAt { get; set; }
    public Guid? SentBy { get; set; }
    public DateTimeOffset? DueDate { get; set; }

    public QuotationGroup QuotationGroup { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
    // 1 approval sekarang fan-out jadi banyak QuotationWorkItem (dikelompokkan per
    // WorkItemName vendor) — jejaknya di sisi QuotationWorkItem.SourceVendorRabRequestId,
    // bukan singular ApprovedWorkItemId di sini lagi.
    public ICollection<QuotationWorkItem> ApprovedWorkItems { get; set; } = [];
    public ICollection<VendorRabRequestLine> Lines { get; set; } = [];
    public ICollection<VendorRabSubmission> Submissions { get; set; } = [];
}

public enum VendorRabRequestStatus
{
    Draft,
    Sent,
    Approved,
    Cancelled,
}
