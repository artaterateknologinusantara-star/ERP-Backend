namespace SynteraERP.Api.DTOs.Auth;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public List<ModulePermissionDto> Permissions { get; set; } = [];
}
