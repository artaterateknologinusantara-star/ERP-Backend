namespace SynteraERP.Api.DTOs.Quotation;

// Input dari VendorRabSubmissionService saat maincon approve submission vendor — QuotationService
// tidak tahu apa pun soal VendorRabRequest/Submission, cuma menerima baris jadi (harga final =
// harga vendor + markup) dan menuliskannya sebagai QuotationWorkItem/WorkDetail resmi lewat jalur
// yang sama seperti WorkItem/WorkDetail manual, termasuk RecalcTotals di transaction yang sama.
public class ApplyVendorRabSubmissionRequest
{
    public Guid QuotationGroupId { get; set; }
    public string WorkItemName { get; set; } = string.Empty;
    public List<ApplyVendorRabSubmissionLine> Lines { get; set; } = [];
}

public class ApplyVendorRabSubmissionLine
{
    public string Name { get; set; } = string.Empty;
    public string? Spesifikasi { get; set; }
    public decimal Volume { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal FinalUnitPrice { get; set; }
    public int SortOrder { get; set; }
}
