namespace SynteraERP.Api.Models;

public class QuotationGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TabId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; } = 0;
    public decimal? RecapVolume { get; set; }
    public string? RecapUnit { get; set; }

    public Guid? SubcontractorId { get; set; }
    public decimal? FinalSubconCost { get; set; }
    public decimal? FinalSellingPrice { get; set; }

    public QuotationTab Tab { get; set; } = null!;
    public Supplier? Subcontractor { get; set; }
    public ICollection<QuotationItem> Items { get; set; } = [];
    public ICollection<QuotationWorkItem> WorkItems { get; set; } = [];
}
