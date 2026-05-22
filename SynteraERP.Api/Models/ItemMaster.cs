using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public class ItemMaster : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public string Uom { get; set; } = string.Empty;
    public string? Warehouse { get; set; }
    public decimal Stock { get; set; }
    public decimal MinStock { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
}
