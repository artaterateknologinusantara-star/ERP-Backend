using System;

namespace SynteraERP.Api.DTOs.ItemMaster;

public class ItemMasterDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public string Uom { get; set; } = string.Empty;
    public string? Warehouse { get; set; }
    public decimal Stock { get; set; }
    public decimal MinStock { get; set; }

    // ── Pricing ──
    public decimal SellingPrice { get; set; }
    public decimal? PurchasePrice { get; set; }
    public decimal? LastPurchasePrice { get; set; }

    // ── Vendor ──
    public Guid? PreferredVendorId { get; set; }
    public string? PreferredVendorName { get; set; }

    // ── Future-ready ──
    public string? Model { get; set; }
    public int? LeadTimeDays { get; set; }
    public string? VendorItemCode { get; set; }
    public string? ProcurementNotes { get; set; }
    public bool IsInventoryItem { get; set; }
    public decimal? ReorderPoint { get; set; }

    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
