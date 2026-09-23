namespace SynteraERP.Api.Models;

public class QuotationItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public string ItemNo { get; set; } = string.Empty;
    public string Equipment { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Manufacturer { get; set; }
    public decimal Qty { get; set; } = 1;
    public string Unit { get; set; } = string.Empty;
    public decimal ServicePrice { get; set; } = 0;
    public decimal MaterialPrice { get; set; } = 0;
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public int SortOrder { get; set; } = 0;

    /// <summary>Link eksplisit ke Item Master — diisi hanya kalau user memilih dari autocomplete
    /// katalog saat mengisi baris ini (lihat CLAUDE.md/00_PROJECT_STATUS.md soal insiden "Server
    /// Blade 2U": tebak-tebakan otomatis berdasarkan kemiripan nama DIHAPUS total, jadi field ini
    /// null kalau baris murni free-text). Dipakai buat propagasi otomatis ke SalesOrderItem saat
    /// Quotation dikonversi ke SO, supaya DO/stok tidak perlu menebak lagi.</summary>
    public Guid? ItemMasterId { get; set; }

    public decimal TotalService => Qty * ServicePrice;
    public decimal TotalMaterial => Qty * MaterialPrice;
    public decimal GrandLine => Qty * (ServicePrice + MaterialPrice);

    public QuotationGroup Group { get; set; } = null!;
    public ItemMaster? ItemMaster { get; set; }
}
