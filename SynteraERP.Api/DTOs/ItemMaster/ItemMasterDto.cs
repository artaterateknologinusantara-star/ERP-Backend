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
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
