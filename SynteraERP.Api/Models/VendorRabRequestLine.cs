using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

// Baris item yang di-draft maincon di dalam sebuah VendorRabRequest — Id-nya adalah "ID
// stabil" yang dipakai sebagai join key di kolom tersembunyi template Excel (bukan posisi
// baris), supaya vendor boleh insert/hapus/urutkan ulang baris di file-nya tanpa salah pasang
// harga. Vendor mengisi UnitPrice lewat VendorRabSubmissionLine yang mereferensikan baris ini.
public class VendorRabRequestLine : BaseEntity
{
    public Guid VendorRabRequestId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Spesifikasi { get; set; }
    public decimal Volume { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public VendorRabRequest VendorRabRequest { get; set; } = null!;
}
