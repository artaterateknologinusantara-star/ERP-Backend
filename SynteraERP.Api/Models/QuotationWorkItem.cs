namespace SynteraERP.Api.Models;

public class QuotationWorkItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; } = 0;

    // Jejak "bagian pekerjaan" ini berasal dari approval VendorRabSubmission yang mana (1
    // approval sekarang fan-out jadi banyak WorkItem, dikelompokkan per WorkItemName vendor) —
    // pindah dari VendorRabRequest.ApprovedWorkItemId (singular) yang tidak cukup lagi untuk
    // melacak banyak WorkItem hasil 1 approval.
    public Guid? SourceVendorRabRequestId { get; set; }

    public QuotationGroup Group { get; set; } = null!;
    public VendorRabRequest? SourceVendorRabRequest { get; set; }
    public ICollection<QuotationWorkDetail> WorkDetails { get; set; } = [];
}
