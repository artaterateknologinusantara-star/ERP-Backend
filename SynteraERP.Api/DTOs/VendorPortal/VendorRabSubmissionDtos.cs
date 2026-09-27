namespace SynteraERP.Api.DTOs.VendorPortal;

public class VendorRabSubmissionDto
{
    public Guid Id { get; set; }
    public Guid VendorRabRequestId { get; set; }
    public int AttemptNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
    public List<VendorRabSubmissionLineDto> Lines { get; set; } = [];
}

public class VendorRabSubmissionLineDto
{
    public Guid Id { get; set; }
    public Guid? VendorRabRequestLineId { get; set; }
    public string? WorkItemName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Spesifikasi { get; set; }
    public decimal Volume { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public decimal ServicePrice { get; set; }
    public decimal MaterialPrice { get; set; }
    public decimal ServiceMarkup { get; set; }
    public decimal MaterialMarkup { get; set; }
    public decimal FinalServicePrice { get; set; }
    public decimal FinalMaterialPrice { get; set; }
    public decimal TotalHarga { get; set; }
    public string? NegotiationNote { get; set; }
}

// Vendor sekarang menyusun RAB-nya sendiri dari nol (nama bagian/item/spesifikasi/volume/
// satuan/harga) — bukan lagi cuma isi harga ke baris yang di-draft maincon. Validasi standar
// (nama/volume/satuan wajib, harga tidak negatif, minimal 1 baris) dicek di
// IVendorRabSubmissionService.CreateAsync, berlaku sama untuk jalur form web maupun Excel.
public class CreateVendorRabSubmissionRequest
{
    public List<CreateVendorRabSubmissionLineRequest> Lines { get; set; } = [];
}

public class CreateVendorRabSubmissionLineRequest
{
    public string? WorkItemName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Spesifikasi { get; set; }
    public decimal Volume { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal ServicePrice { get; set; }
    public decimal MaterialPrice { get; set; }
}

public class SetSubmissionLineMarkupRequest
{
    public decimal ServiceMarkup { get; set; }
    public decimal MaterialMarkup { get; set; }
}

public class RejectVendorRabSubmissionRequest
{
    public string? Reason { get; set; }
}

// "Minta Revisi" — beda dari Reject total: submission TIDAK ditolak seluruhnya, cuma baris
// tertentu yang di-flag maincon untuk dinego. Tidak membuat attempt baru (bukan clone-forward
// seperti QuotationService.CreateRevisionAsync) — vendor edit ulang baris yang di-flag lalu
// submit sebagai attempt berikutnya sendiri, persis seperti alur Reject yang sudah ada.
public class RequestVendorRabRevisionRequest
{
    public List<RequestVendorRabRevisionLine> Lines { get; set; } = [];
}

public class RequestVendorRabRevisionLine
{
    public Guid LineId { get; set; }
    public string Note { get; set; } = string.Empty;
}
