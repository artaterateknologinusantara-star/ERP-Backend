using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

// Baris RAB milik vendor sendiri — Nama/Spesifikasi/Volume/Unit sekarang diisi/dipegang vendor
// di sini langsung (bukan lagi dikunci dari VendorRabRequestLine yang di-draft maincon), supaya
// vendor bebas tambah/hapus/urutkan baris di attempt-nya sendiri. VendorRabRequestLineId jadi
// opsional — tetap ada untuk baris lama dari sebelum perubahan ini, null untuk baris baru.
// ServicePrice/MaterialPrice dipecah (task #44 Bagian 2, Opsi B) sejak vendor sendiri yang jadi
// sumber breakdown-nya, bukan direalokasi belakangan oleh maincon. ServiceMarkup/MaterialMarkup
// diisi maincon saat review sebelum approve — harga resmi yang ditulis ke
// QuotationWorkDetail.ServicePrice/MaterialPrice adalah (ServicePrice+ServiceMarkup) dan
// (MaterialPrice+MaterialMarkup). NegotiationNote diisi maincon saat "Minta Revisi" untuk
// menandai baris mana yang perlu diubah vendor pada attempt berikutnya (clone-forward, lihat
// pola QuotationService.CreateRevisionAsync).
public class VendorRabSubmissionLine : BaseEntity
{
    public Guid VendorRabSubmissionId { get; set; }
    public Guid? VendorRabRequestLineId { get; set; }
    public string Name { get; set; } = string.Empty;
    // Vendor mengelompokkan barisnya sendiri jadi beberapa "bagian pekerjaan" — kosong/null
    // berarti baris ini masuk kelompok default (nama diambil dari VendorRabRequest.Name) saat
    // fan-out ke QuotationWorkItem di approval.
    public string? WorkItemName { get; set; }
    public string? Spesifikasi { get; set; }
    public decimal Volume { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public decimal ServicePrice { get; set; }
    public decimal MaterialPrice { get; set; }
    public decimal ServiceMarkup { get; set; } = 0;
    public decimal MaterialMarkup { get; set; } = 0;
    public string? NegotiationNote { get; set; }

    public VendorRabSubmission VendorRabSubmission { get; set; } = null!;
    public VendorRabRequestLine? VendorRabRequestLine { get; set; }
}
