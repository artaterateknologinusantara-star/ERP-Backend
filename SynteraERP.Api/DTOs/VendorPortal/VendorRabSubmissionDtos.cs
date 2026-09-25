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
    public Guid VendorRabRequestLineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Spesifikasi { get; set; }
    public decimal Volume { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal MarkupAmount { get; set; }
    public decimal FinalUnitPrice { get; set; }
    public decimal TotalHarga { get; set; }
}

// Vendor kirim harga untuk SEMUA baris VendorRabRequestLine dalam 1 kali submit — sama seperti
// prinsip reject-all-on-error di import Excel (bagian 5 dokumen rencana), supaya jalur form web
// dan Excel divalidasi dengan aturan yang sama persis: baris hilang/tidak dikenal ditolak semua.
public class CreateVendorRabSubmissionRequest
{
    public List<CreateVendorRabSubmissionLineRequest> Lines { get; set; } = [];
}

public class CreateVendorRabSubmissionLineRequest
{
    public Guid VendorRabRequestLineId { get; set; }
    public decimal UnitPrice { get; set; }
}

public class SetSubmissionLineMarkupRequest
{
    public decimal MarkupAmount { get; set; }
}

public class RejectVendorRabSubmissionRequest
{
    public string? Reason { get; set; }
}
