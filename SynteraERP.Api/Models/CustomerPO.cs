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

public class CustomerPoHistory
{
    public Guid Id { get; set; }
    public Guid CustomerPoId { get; set; }
    public string OldPoNo { get; set; } = string.Empty;
    public string NewPoNo { get; set; } = string.Empty;
    public Guid? ChangedBy { get; set; }
    public string? ChangedByName { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Reason { get; set; }

    public CustomerPO CustomerPO { get; set; } = null!;
}
