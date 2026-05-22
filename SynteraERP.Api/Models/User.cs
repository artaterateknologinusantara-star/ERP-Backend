using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public class User : BaseEntity
{
    public Guid RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAt { get; set; }

    public Role Role { get; set; } = null!;
}
