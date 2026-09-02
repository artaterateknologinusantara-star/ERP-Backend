using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public class DemoLead : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string WhatsappNumber { get; set; } = string.Empty;
    public string CompanyEmail { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string Need { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DemoLeadStatus Status { get; set; } = DemoLeadStatus.New;
}

public enum DemoLeadStatus
{
    New,
    Contacted,
    Converted,
    Rejected,
}
