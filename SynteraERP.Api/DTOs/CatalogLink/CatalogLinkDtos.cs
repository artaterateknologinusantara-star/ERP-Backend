namespace SynteraERP.Api.DTOs.CatalogLink;

/// <summary>Satu baris Quotation/Sales Order yang belum ada ItemMasterId — dipakai layar admin
/// "Item Belum Terhubung" untuk beres-beres data lama (dibuat sebelum field ItemMasterId ada di
/// QuotationItem, atau dari SO yang dibuat sebelum fallback tebak-nama dihapus). Tidak ada
/// auto-backfill fuzzy apa pun di sini — link selalu dipilih manual oleh admin per baris.</summary>
public class UnlinkedCatalogItemDto
{
    public Guid ItemRowId { get; set; }
    public string SourceType { get; set; } = string.Empty; // "Quotation" | "SalesOrder"
    public string SourceNo { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public decimal Qty { get; set; }
    public string Uom { get; set; } = string.Empty;
}

public class LinkCatalogItemRequest
{
    public Guid ItemMasterId { get; set; }
}
