using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public class Quotation : BaseEntity
{
    public string No { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public DateOnly ValidUntil { get; set; }
    public Guid SalesId { get; set; }
    public int Revision { get; set; } = 0;
    public Guid? ParentId { get; set; }
    public QuotationStatus Status { get; set; } = QuotationStatus.Draft;
    public decimal Discount { get; set; } = 0;
    public decimal TaxRate { get; set; } = 11;
    public decimal TotalMaterial { get; set; } = 0;
    public decimal TotalService { get; set; } = 0;
    public decimal TotalBeforeTax { get; set; } = 0;
    public decimal TaxAmount { get; set; } = 0;
    public decimal GrandTotal { get; set; } = 0;
    public string? PaymentTerms { get; set; }
    public string? Notes { get; set; }
    public string? AdditionalNotes { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public bool IsLatestRevision { get; set; } = true;
    public DateTimeOffset? SentAt { get; set; }
    public Guid? SentBy { get; set; }
    public Guid? SupersededByQuotationId { get; set; }

    public Customer Customer { get; set; } = null!;
    public User Sales { get; set; } = null!;
    public Quotation? Parent { get; set; }
    public Quotation? SupersededBy { get; set; }
    public ICollection<QuotationTab> Tabs { get; set; } = [];
}

public enum QuotationStatus
{
    Draft,
    Terkirim,
    Disetujui,
    Ditolak,
    Kadaluarsa,
    Direvisi,
    Selesai,
    Superseded,
}
