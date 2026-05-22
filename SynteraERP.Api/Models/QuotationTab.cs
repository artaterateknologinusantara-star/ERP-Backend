namespace SynteraERP.Api.Models;

public class QuotationTab
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuotationId { get; set; }
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; } = 0;

    public Quotation Quotation { get; set; } = null!;
    public ICollection<QuotationGroup> Groups { get; set; } = [];
}
