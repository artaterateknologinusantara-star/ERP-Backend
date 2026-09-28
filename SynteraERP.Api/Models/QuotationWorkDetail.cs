namespace SynteraERP.Api.Models;

public class QuotationWorkDetail
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Spesifikasi { get; set; }
    public decimal Volume { get; set; } = 0;
    public string Unit { get; set; } = string.Empty;
    public decimal ServicePrice { get; set; } = 0;
    public decimal MaterialPrice { get; set; } = 0;
    public int SortOrder { get; set; } = 0;

    public decimal TotalHarga => Volume * (ServicePrice + MaterialPrice);

    public QuotationWorkItem WorkItem { get; set; } = null!;
    public ICollection<QuotationWorkDetailAttachment> Attachments { get; set; } = [];
}
