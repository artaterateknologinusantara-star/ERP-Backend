using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

// Harga satuan yang diisi vendor untuk 1 VendorRabRequestLine, dalam 1 percobaan submission —
// dipecah Jasa/Material (task #44 Bagian 2, Opsi B) sejak vendor sendiri yang jadi sumber
// breakdown-nya, bukan direalokasi belakangan oleh maincon. ServiceMarkup/MaterialMarkup diisi
// maincon saat review sebelum approve — harga resmi yang ditulis ke
// QuotationWorkDetail.ServicePrice/MaterialPrice adalah (ServicePrice+ServiceMarkup) dan
// (MaterialPrice+MaterialMarkup).
public class VendorRabSubmissionLine : BaseEntity
{
    public Guid VendorRabSubmissionId { get; set; }
    public Guid VendorRabRequestLineId { get; set; }
    public decimal ServicePrice { get; set; }
    public decimal MaterialPrice { get; set; }
    public decimal ServiceMarkup { get; set; } = 0;
    public decimal MaterialMarkup { get; set; } = 0;

    public VendorRabSubmission VendorRabSubmission { get; set; } = null!;
    public VendorRabRequestLine VendorRabRequestLine { get; set; } = null!;
}
