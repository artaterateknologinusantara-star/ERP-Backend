using System.ComponentModel.DataAnnotations;

namespace SynteraERP.Api.DTOs.Auth;

public class ModulePermissionDto
{
    public string Module { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanApprove { get; set; }
}

public class RoleListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int UserCount { get; set; }
    public List<ModulePermissionDto> Permissions { get; set; } = [];
}

public class CreateRoleRequest
{
    [Required] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateRoleRequest
{
    [Required] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdatePermissionsRequest
{
    [Required] public List<ModulePermissionDto> Permissions { get; set; } = [];
}
