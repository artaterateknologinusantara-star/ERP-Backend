using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public class Customer : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Npwp { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Quotation> Quotations { get; set; } = [];
    public ICollection<SalesOrder> SalesOrders { get; set; } = [];
    public ICollection<Invoice> Invoices { get; set; } = [];
}
