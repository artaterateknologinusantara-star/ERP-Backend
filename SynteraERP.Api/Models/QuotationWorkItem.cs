namespace SynteraERP.Api.Models;

public class QuotationWorkItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; } = 0;

    public QuotationGroup Group { get; set; } = null!;
    public ICollection<QuotationWorkDetail> WorkDetails { get; set; } = [];
}
