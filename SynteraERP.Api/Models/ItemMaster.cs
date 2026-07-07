using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public class ItemMaster : BaseEntity
{
    public string Code        { get; set; } = string.Empty;
    public string Name        { get; set; } = string.Empty;
    public string? Description{ get; set; }
    public string? Category   { get; set; }
    public string? Brand      { get; set; }
    public string Uom         { get; set; } = string.Empty;
    public string? Warehouse  { get; set; }
    public decimal Stock      { get; set; }
    public decimal MinStock   { get; set; }

    // ── Pricing (Fase 1) ──
    public decimal SellingPrice       { get; set; }
    public decimal? PurchasePrice     { get; set; }
    public decimal? LastPurchasePrice { get; set; }

    // ── Moving Average Cost (Fase 3 Accounting) ──
    public decimal CurrentAverageCost { get; set; } = 0;

    // ── Vendor (Fase 1) ──
    public Guid? PreferredVendorId { get; set; }
    public Supplier? PreferredVendor { get; set; }

    // ── Future-ready fields ──
    public string? Model            { get; set; }
    public int? LeadTimeDays        { get; set; }
    public string? VendorItemCode   { get; set; }
    public string? ProcurementNotes { get; set; }
    public bool IsInventoryItem     { get; set; } = true;
    public decimal? ReorderPoint    { get; set; }

    public bool IsActive { get; set; } = true;
}
