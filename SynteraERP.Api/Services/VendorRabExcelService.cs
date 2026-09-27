using ClosedXML.Excel;
using SynteraERP.Api.DTOs.VendorPortal;

namespace SynteraERP.Api.Services;

// Template kosong sepenuhnya — subcon adalah pihak yang menyusun RAB sesungguhnya (nama
// bagian/item/spesifikasi/volume/satuan/harga), bukan cuma isi harga ke baris yang sudah
// di-draft maincon (lihat perubahan arah task RAB Sep 2026). Tidak ada lagi kolom ID
// tersembunyi/join-key — semua baris di file memang baru, tidak mencocokkan ke baris lama.
public static class VendorRabExcelService
{
    private const int ColWorkItemName = 1;
    private const int ColName = 2;
    private const int ColSpec = 3;
    private const int ColVolume = 4;
    private const int ColUnit = 5;
    private const int ColServicePrice = 6;
    private const int ColMaterialPrice = 7;

    public static byte[] GenerateTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("RAB");

        ws.Cell(1, ColWorkItemName).Value = "Nama Bagian/Pekerjaan";
        ws.Cell(1, ColName).Value = "Nama Item";
        ws.Cell(1, ColSpec).Value = "Spesifikasi";
        ws.Cell(1, ColVolume).Value = "Volume";
        ws.Cell(1, ColUnit).Value = "Satuan";
        ws.Cell(1, ColServicePrice).Value = "Harga Jasa";
        ws.Cell(1, ColMaterialPrice).Value = "Harga Material";
        ws.Row(1).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }

    // Reject-all di level parsing file: kalau ADA satu saja baris rusak/tidak terbaca, seluruh
    // file ditolak (tidak ada partial-import) — konsisten dengan pola BankReconciliation CSV
    // import. Baris yang lolos parsing di sini masih divalidasi lagi secara semantik (ID
    // dikenal/lengkap/tidak duplikat) oleh IVendorRabSubmissionService.CreateAsync, persis
    // seperti jalur form web — supaya kedua jalur input divalidasi dengan aturan yang sama.
    public static (CreateVendorRabSubmissionRequest? request, List<string> errors) ParseSubmission(Stream fileStream)
    {
        var errors = new List<string>();
        var lines = new List<CreateVendorRabSubmissionLineRequest>();

        XLWorkbook wb;
        try
        {
            wb = new XLWorkbook(fileStream);
        }
        catch (Exception)
        {
            return (null, ["File tidak bisa dibaca sebagai Excel (.xlsx) yang valid."]);
        }

        using (wb)
        {
            var ws = wb.Worksheets.FirstOrDefault();
            if (ws is null)
                return (null, ["File Excel tidak punya sheet sama sekali."]);

            var lastRowUsed = ws.LastRowUsed()?.RowNumber() ?? 1;
            for (var row = 2; row <= lastRowUsed; row++)
            {
                var workItemName = ws.Cell(row, ColWorkItemName).GetString().Trim();
                var name = ws.Cell(row, ColName).GetString().Trim();
                var spec = ws.Cell(row, ColSpec).GetString().Trim();
                var unit = ws.Cell(row, ColUnit).GetString().Trim();
                var volumeCell = ws.Cell(row, ColVolume);
                var servicePriceCell = ws.Cell(row, ColServicePrice);
                var materialPriceCell = ws.Cell(row, ColMaterialPrice);

                // Baris kosong total dilewati diam-diam — bukan error, ini cuma baris kosong sisa
                // di bawah data (umum terjadi di Excel).
                if (workItemName.Length == 0 && name.Length == 0 && spec.Length == 0 && unit.Length == 0
                    && volumeCell.IsEmpty() && servicePriceCell.IsEmpty() && materialPriceCell.IsEmpty())
                    continue;

                if (name.Length == 0)
                {
                    errors.Add($"Baris {row}: Nama Item tidak boleh kosong.");
                    continue;
                }

                if (volumeCell.IsEmpty() || !volumeCell.TryGetValue<decimal>(out var volume) || volume <= 0)
                {
                    errors.Add($"Baris {row}: Volume kosong, bukan angka, atau tidak lebih dari 0.");
                    continue;
                }

                if (unit.Length == 0)
                {
                    errors.Add($"Baris {row}: Satuan tidak boleh kosong.");
                    continue;
                }

                if (servicePriceCell.IsEmpty() || !servicePriceCell.TryGetValue<decimal>(out var servicePrice))
                {
                    errors.Add($"Baris {row}: Harga Jasa kosong atau bukan angka.");
                    continue;
                }

                if (materialPriceCell.IsEmpty() || !materialPriceCell.TryGetValue<decimal>(out var materialPrice))
                {
                    errors.Add($"Baris {row}: Harga Material kosong atau bukan angka.");
                    continue;
                }

                if (servicePrice < 0 || materialPrice < 0)
                {
                    errors.Add($"Baris {row}: Harga Jasa/Material tidak boleh negatif.");
                    continue;
                }

                lines.Add(new CreateVendorRabSubmissionLineRequest
                {
                    WorkItemName = workItemName.Length > 0 ? workItemName : null,
                    Name = name,
                    Spesifikasi = spec.Length > 0 ? spec : null,
                    Volume = volume,
                    Unit = unit,
                    ServicePrice = servicePrice,
                    MaterialPrice = materialPrice,
                });
            }
        }

        if (errors.Count > 0) return (null, errors);
        if (lines.Count == 0) return (null, ["File tidak berisi baris apa pun."]);

        return (new CreateVendorRabSubmissionRequest { Lines = lines }, []);
    }
}
