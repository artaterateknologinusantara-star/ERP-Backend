using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<User> Users { get; set; } = [];
}
