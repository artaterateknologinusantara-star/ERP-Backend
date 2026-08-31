using Microsoft.AspNetCore.Authorization;

namespace SynteraERP.Api.Authorization;

public class ModulePermissionRequirement(string module, string action) : IAuthorizationRequirement
{
    public string Module { get; } = module;
    public string Action { get; } = action;

    public string PolicyName => $"{Module}:{Action}";
}
