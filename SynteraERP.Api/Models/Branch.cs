using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public class Branch : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Manager { get; set; }
    public bool IsActive { get; set; } = true;
}
