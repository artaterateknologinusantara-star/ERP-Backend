using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public class ExpenseCategory : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid AccountId { get; set; }
    public bool IsActive { get; set; } = true;

    public Account Account { get; set; } = null!;
}
