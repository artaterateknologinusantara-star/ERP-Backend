using ClosedXML.Excel;
using SynteraERP.Api.DTOs.VendorPortal;

namespace SynteraERP.Api.Services;

// Template kosong sepenuhnya — subcon adalah pihak yang menyusun RAB sesungguhnya (nama
// bagian/item/spesifikasi/volume/satuan/harga), bukan cuma isi harga ke baris yang sudah
// di-draft maincon (lihat perubahan arah task RAB Sep 2026). Tidak ada lagi kolom ID
// tersembunyi/join-key — semua baris di file memang baru, tidak mencocokkan ke baris lama.
//
// Layout mengikuti format BOQ yang sudah dipakai vendor di lapangan (bukan 1 baris header
// rata dengan "Nama Bagian/Pekerjaan" diulang tiap baris) — dikonfirmasi product owner
// Sep 2026, MENGGANTIKAN total layout lama:
//   Baris 1-2 = header 2 tingkat (No. | Scope of Works | Specification | Unit | Qty |
//               Price per Unit [Jasa & Instalasi, Material] | Price Total [Jasa & Instalasi,
//               Material] — 2 kolom terakhir cuma rumus tampilan, tidak dibaca saat import).
//   Baris section = kolom "No." diisi teks non-angka (mis. "A. Nama Bagian/Pekerjaan"),
//               menandai bagian/pekerjaan baru untuk baris-baris item di bawahnya. Kolom lain
//               di baris ini (termasuk kalau vendor isi Unit/Qty rekap) diabaikan — tidak ada
//               field tujuan untuk itu di model saat ini.
//   Baris item  = kolom "No." diisi angka urut (1, 2, 3, ...); Nama Item ada di kolom
//               "Scope of Works", Spesifikasi/Unit/Qty/Harga Jasa/Harga Material di kolom
//               berikutnya.
//   Baris kosong di kolom "No." (baris "Total" rekap section, baris kosong sisa, catatan) —
//               dilewati diam-diam, tidak pernah jadi baris data maupun error.
public static class VendorRabExcelService
{
    private const int ColNo = 1;
    private const int ColName = 2; // "Scope of Works": nama section (baris section) / nama item (baris item)
    private const int ColSpec = 3;
    private const int ColUnit = 4;
    private const int ColVolume = 5;
    private const int ColServicePrice = 6;
    private const int ColMaterialPrice = 7;
    private const int ColPriceTotalService = 8; // rumus tampilan saja, tidak dibaca saat import
    private const int ColPriceTotalMaterial = 9; // rumus tampilan saja, tidak dibaca saat import

    public static byte[] GenerateTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("RAB");

        ws.Cell(1, ColNo).Value = "No.";
        ws.Cell(1, ColName).Value = "Scope of Works";
        ws.Cell(1, ColSpec).Value = "Specification";
        ws.Cell(1, ColUnit).Value = "Unit";
        ws.Cell(1, ColVolume).Value = "Qty";
        ws.Cell(1, ColServicePrice).Value = "Price per Unit";
        ws.Cell(1, ColPriceTotalService).Value = "Price Total";

        ws.Cell(2, ColServicePrice).Value = "Jasa & Instalasi";
        ws.Cell(2, ColMaterialPrice).Value = "Material";
        ws.Cell(2, ColPriceTotalService).Value = "Jasa & Instalasi";
        ws.Cell(2, ColPriceTotalMaterial).Value = "Material";

        ws.Range(1, ColNo, 2, ColNo).Merge();
        ws.Range(1, ColName, 2, ColName).Merge();
        ws.Range(1, ColSpec, 2, ColSpec).Merge();
        ws.Range(1, ColUnit, 2, ColUnit).Merge();
        ws.Range(1, ColVolume, 2, ColVolume).Merge();
        ws.Range(1, ColServicePrice, 1, ColMaterialPrice).Merge();
        ws.Range(1, ColPriceTotalService, 1, ColPriceTotalMaterial).Merge();

        ws.Row(1).Style.Font.Bold = true;
        ws.Row(2).Style.Font.Bold = true;
        ws.SheetView.FreezeRows(2);

        // Baris contoh — bukan data (kolom "No." diisi teks, bukan angka, jadi baris ini
        // sendiri dilewati saat import), cuma menjelaskan konvensi ke vendor.
        ws.Cell(3, ColNo).Value = "A. Nama Bagian/Pekerjaan (ganti/hapus sesuai kebutuhan)";
        ws.Cell(3, ColNo).Style.Font.Italic = true;
        ws.Cell(3, ColNo).Style.Font.FontColor = XLColor.Gray;

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

            string? currentSection = null;
            var lastRowUsed = ws.LastRowUsed()?.RowNumber() ?? 1;

            // Data dimulai baris 3 (baris 1-2 = header 2 tingkat).
            for (var row = 3; row <= lastRowUsed; row++)
            {
                var noCell = ws.Cell(row, ColNo);

                // Kolom "No." kosong = bukan baris data sama sekali (baris "Total" rekap
                // section, baris kosong sisa, catatan bebas) — dilewati diam-diam, tidak pernah
                // divalidasi atau dianggap error.
                if (noCell.IsEmpty())
                    continue;

                // Kolom "No." isinya angka → baris item. Isinya teks (mis. "A. Nama Bagian/
                // Pekerjaan") → baris section: catat sebagai WorkItemName untuk baris item di
                // bawahnya, kolom lain di baris ini (termasuk rekap Unit/Qty bila vendor isi)
                // sengaja diabaikan — tidak ada field tujuan untuk itu di model saat ini.
                if (!noCell.TryGetValue<double>(out _))
                {
                    currentSection = noCell.GetString().Trim();
                    continue;
                }

                var name = ws.Cell(row, ColName).GetString().Trim();
                var spec = ws.Cell(row, ColSpec).GetString().Trim();
                var unit = ws.Cell(row, ColUnit).GetString().Trim();
                var volumeCell = ws.Cell(row, ColVolume);
                var servicePriceCell = ws.Cell(row, ColServicePrice);
                var materialPriceCell = ws.Cell(row, ColMaterialPrice);

                if (name.Length == 0)
                {
                    errors.Add($"Baris {row}: Nama Item (kolom Scope of Works) tidak boleh kosong.");
                    continue;
                }

                if (volumeCell.IsEmpty() || !volumeCell.TryGetValue<decimal>(out var volume) || volume <= 0)
                {
                    errors.Add($"Baris {row}: Qty kosong, bukan angka, atau tidak lebih dari 0.");
                    continue;
                }

                if (unit.Length == 0)
                {
                    errors.Add($"Baris {row}: Unit tidak boleh kosong.");
                    continue;
                }

                if (servicePriceCell.IsEmpty() || !servicePriceCell.TryGetValue<decimal>(out var servicePrice))
                {
                    errors.Add($"Baris {row}: Harga Jasa (Price per Unit - Jasa & Instalasi) kosong atau bukan angka.");
                    continue;
                }

                if (materialPriceCell.IsEmpty() || !materialPriceCell.TryGetValue<decimal>(out var materialPrice))
                {
                    errors.Add($"Baris {row}: Harga Material (Price per Unit - Material) kosong atau bukan angka.");
                    continue;
                }

                if (servicePrice < 0 || materialPrice < 0)
                {
                    errors.Add($"Baris {row}: Harga Jasa/Material tidak boleh negatif.");
                    continue;
                }

                lines.Add(new CreateVendorRabSubmissionLineRequest
                {
                    WorkItemName = currentSection,
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
