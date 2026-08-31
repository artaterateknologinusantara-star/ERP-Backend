using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public class Permission : BaseEntity
{
    public Guid RoleId { get; set; }
    public string Module { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanApprove { get; set; }

    public Role Role { get; set; } = null!;
}

// Canonical module keys, matching the sidebar's top-level nav groups. Kept as plain string
// constants (not an enum) because Permission.Module is persisted as a string — new modules can
// be added here without a migration to change a column type.
public static class Modules
{
    public const string Sales = "Sales";
    public const string Purchasing = "Purchasing";
    public const string Finance = "Finance";
    public const string Accounting = "Accounting";
    public const string Inventory = "Inventory";
    public const string Project = "Project";
    public const string Settings = "Settings";

    public static readonly string[] All =
    [
        Sales, Purchasing, Finance, Accounting, Inventory, Project, Settings,
    ];
}

public static class PermissionActions
{
    public const string View = "View";
    public const string Create = "Create";
    public const string Edit = "Edit";
    public const string Delete = "Delete";
    public const string Approve = "Approve";

    public static readonly string[] All = [View, Create, Edit, Delete, Approve];
}
