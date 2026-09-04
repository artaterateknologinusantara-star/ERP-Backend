using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public class ProjectRevenueRecognition : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public DateTimeOffset RecognitionDate { get; set; }
    public decimal ActualCostToDate { get; set; }
    public decimal PercentageComplete { get; set; }
    public decimal CumulativeRevenueRecognized { get; set; }
    public decimal IncrementalRevenueThisEntry { get; set; }
    public Guid? JournalEntryId { get; set; }
}
