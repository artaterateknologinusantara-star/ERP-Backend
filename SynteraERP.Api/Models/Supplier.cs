using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public class Supplier : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Npwp { get; set; }
    public string? BankName { get; set; }
    public string? BankAccount { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = [];
}
