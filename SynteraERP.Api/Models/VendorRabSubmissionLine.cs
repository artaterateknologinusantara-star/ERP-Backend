using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

// Harga satuan yang diisi vendor untuk 1 VendorRabRequestLine, dalam 1 percobaan submission.
// MarkupAmount (per unit, Rupiah) diisi maincon saat review sebelum approve — harga resmi yang
// ditulis ke QuotationWorkDetail.UnitPrice adalah UnitPrice + MarkupAmount.
public class VendorRabSubmissionLine : BaseEntity
{
    public Guid VendorRabSubmissionId { get; set; }
    public Guid VendorRabRequestLineId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal MarkupAmount { get; set; } = 0;

    public VendorRabSubmission VendorRabSubmission { get; set; } = null!;
    public VendorRabRequestLine VendorRabRequestLine { get; set; } = null!;
}
