using Microsoft.AspNetCore.Authorization;

namespace SynteraERP.Api.Authorization;

// Usage: [RequirePermission(Modules.Sales, PermissionActions.Approve)]
// Maps to an AddPolicy("Sales:Approve", ...) registered in Program.cs, backed by
// ModulePermissionHandler which checks the caller's role against the Permissions table.
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string module, string action)
    {
        Policy = new ModulePermissionRequirement(module, action).PolicyName;
    }
}
