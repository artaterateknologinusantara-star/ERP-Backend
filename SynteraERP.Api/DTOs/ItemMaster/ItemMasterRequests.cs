namespace SynteraERP.Api.DTOs.ItemMaster;

public class CreateItemMasterRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public string Uom { get; set; } = string.Empty;
    public string? Warehouse { get; set; }
    public decimal Stock { get; set; }
    public decimal MinStock { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal? PurchasePrice { get; set; }
    public Guid? PreferredVendorId { get; set; }
    public string? Model { get; set; }
    public int? LeadTimeDays { get; set; }
    public string? VendorItemCode { get; set; }
    public string? ProcurementNotes { get; set; }
    public decimal? ReorderPoint { get; set; }
}

public class UpdateItemMasterRequest : CreateItemMasterRequest { }
