using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

// 1 percobaan submit vendor untuk 1 VendorRabRequest. Versioning: kalau di-Reject, vendor
// submit lagi sebagai AttemptNumber baru (submission lama tetap tersimpan sebagai histori,
// tidak diedit ulang) — lihat keputusan produk 24 Sep 2026. Hanya 1 submission per Request
// yang boleh berstatus PendingReview di waktu yang sama (dicek di service layer).
public class VendorRabSubmission : BaseEntity
{
    public Guid VendorRabRequestId { get; set; }
    public int AttemptNumber { get; set; }
    public VendorRabSubmissionStatus Status { get; set; } = VendorRabSubmissionStatus.PendingReview;
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid SubmittedByPortalUserId { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }

    public VendorRabRequest VendorRabRequest { get; set; } = null!;
    public SupplierPortalUser SubmittedByPortalUser { get; set; } = null!;
    public ICollection<VendorRabSubmissionLine> Lines { get; set; } = [];
}

public enum VendorRabSubmissionStatus
{
    PendingReview,
    Approved,
    Rejected,
}
