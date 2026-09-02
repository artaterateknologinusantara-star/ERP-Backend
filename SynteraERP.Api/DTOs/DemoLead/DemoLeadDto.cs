namespace SynteraERP.Api.DTOs.DemoLead;

public class DemoLeadDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string WhatsappNumber { get; set; } = string.Empty;
    public string CompanyEmail { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string Need { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public class CreateDemoLeadRequest
{
    public string FullName { get; set; } = string.Empty;
    public string WhatsappNumber { get; set; } = string.Empty;
    public string CompanyEmail { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string Need { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class UpdateDemoLeadStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
