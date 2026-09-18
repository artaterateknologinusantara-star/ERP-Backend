namespace SynteraERP.Api.Models;

public class QuotationTermin
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuotationId { get; set; }
    public int SortOrder { get; set; } = 0;
    public string Description { get; set; } = string.Empty;
    public decimal Percentage { get; set; }

    public Quotation Quotation { get; set; } = null!;
}
