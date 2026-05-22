using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public class CustomerPO : BaseEntity
{
    public string PoNo { get; set; } = string.Empty;
    public Guid QuotationId { get; set; }
    public DateOnly PoDate { get; set; }
    public decimal Amount { get; set; } = 0;
    public string? Notes { get; set; }
    public string? AttachmentPath { get; set; }
    public string? AttachmentName { get; set; }

    public Quotation Quotation { get; set; } = null!;
}
