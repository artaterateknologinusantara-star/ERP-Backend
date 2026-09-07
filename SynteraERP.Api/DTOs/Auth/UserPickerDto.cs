namespace SynteraERP.Api.DTOs.Auth;

// Minimal projection for cross-module "assign to" pickers (Sales Order, Quotation) - deliberately
// excludes Email/Role so any authenticated user can resolve a name from an id without leaking
// who has which role. Contrast with UserProfileDto, which is the full profile shape for Me()/UserController.
public class UserPickerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
