namespace SynteraERP.Api.DTOs.Approval;

public class PendingApprovalDto
{
    public Guid Id { get; set; }
    public string Module { get; set; } = string.Empty;
    // Matches the DTO/service each item's approve/reject action is routed through on the frontend.
    public string Type { get; set; } = string.Empty;
    public string TypeLabel { get; set; } = string.Empty;
    public string No { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string RequestedByName { get; set; } = string.Empty;
    public string? DetailUrl { get; set; }
}
