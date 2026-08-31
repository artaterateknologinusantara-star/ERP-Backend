using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.Models;

namespace SynteraERP.Api.Authorization;

public class ModulePermissionHandler(AppDbContext db) : AuthorizationHandler<ModulePermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ModulePermissionRequirement requirement)
    {
        var roleIdStr = context.User.FindFirst("roleId")?.Value;
        if (roleIdStr is null || !Guid.TryParse(roleIdStr, out var roleId))
            return;

        var perm = await db.Permissions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.RoleId == roleId && p.Module == requirement.Module);

        if (perm is null) return;

        var allowed = requirement.Action switch
        {
            PermissionActions.View => perm.CanView,
            PermissionActions.Create => perm.CanCreate,
            PermissionActions.Edit => perm.CanEdit,
            PermissionActions.Delete => perm.CanDelete,
            PermissionActions.Approve => perm.CanApprove,
            _ => false,
        };

        if (allowed) context.Succeed(requirement);
    }
}
