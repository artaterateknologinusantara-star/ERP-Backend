namespace SynteraERP.Api.DTOs.Quotation;

// Input dari VendorRabSubmissionService saat maincon approve submission vendor — QuotationService
// tidak tahu apa pun soal VendorRabSubmission, cuma menerima baris jadi (harga final per kategori
// = harga vendor + markup, sudah dipecah Jasa/Material sejak vendor submit — task #44 Bagian 2
// Opsi B) dan menuliskannya sebagai QuotationWorkItem/WorkDetail resmi lewat jalur yang sama
// seperti WorkItem/WorkDetail manual, termasuk RecalcTotals di transaction yang sama.
//
// Vendor mengelompokkan barisnya sendiri jadi beberapa "bagian pekerjaan" (WorkItemName per
// baris) — 1 approval sekarang fan-out jadi SATU QuotationWorkItem per kelompok, bukan 1 WorkItem
// untuk semua baris. Baris tanpa WorkItemName (null/kosong) masuk 1 kelompok default bernama
// DefaultWorkItemName, supaya tidak ada baris yang hilang dari fan-out.
public class ApplyVendorRabSubmissionRequest
{
    public Guid QuotationGroupId { get; set; }
    public Guid SourceVendorRabRequestId { get; set; }
    public string DefaultWorkItemName { get; set; } = string.Empty;
    public List<ApplyVendorRabSubmissionLine> Lines { get; set; } = [];
}

public class ApplyVendorRabSubmissionLine
{
    public string? WorkItemName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Spesifikasi { get; set; }
    public decimal Volume { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal FinalServicePrice { get; set; }
    public decimal FinalMaterialPrice { get; set; }
    public int SortOrder { get; set; }
}
