namespace SynteraERP.Api.Models;

public class QuotationWorkDetailAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkDetailId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public int SortOrder { get; set; } = 0;

    public QuotationWorkDetail WorkDetail { get; set; } = null!;
}
