using ClosedXML.Excel;
using SynteraERP.Api.DTOs.VendorPortal;

namespace SynteraERP.Api.Services;

// Template per VendorRabRequest: kolom A (ID baris, GUID) di-hide + dikunci, kolom Nama/
// Spesifikasi/Volume/Satuan dikunci (read-only), hanya kolom Harga Satuan yang bisa diisi
// vendor. Kolom ID adalah join key saat parse balik — BUKAN posisi baris — supaya vendor boleh
// menyisipkan/menghapus/mengurutkan ulang baris tanpa salah pasang harga (lihat bagian 5
// dokumen rencana). Proteksi sheet ini cuma UX nicety (file .xlsx bisa saja di-unprotect
// manual) — batas keamanan yang sesungguhnya adalah validasi ID di server saat parse balik,
// sama seperti CreateVendorRabSubmissionRequest lewat form web.
public static class VendorRabExcelService
{
    private const int ColId = 1;
    private const int ColName = 2;
    private const int ColSpec = 3;
    private const int ColVolume = 4;
    private const int ColUnit = 5;
    private const int ColUnitPrice = 6;

    public static byte[] GenerateTemplate(VendorRabRequestDto request)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("RAB");

        ws.Cell(1, ColId).Value = "ID (jangan diubah)";
        ws.Cell(1, ColName).Value = "Nama Pekerjaan";
        ws.Cell(1, ColSpec).Value = "Spesifikasi";
        ws.Cell(1, ColVolume).Value = "Volume";
        ws.Cell(1, ColUnit).Value = "Satuan";
        ws.Cell(1, ColUnitPrice).Value = "Harga Satuan (isi di sini)";
        ws.Row(1).Style.Font.Bold = true;

        var row = 2;
        foreach (var line in request.Lines.OrderBy(l => l.SortOrder))
        {
            ws.Cell(row, ColId).Value = line.Id.ToString();
            ws.Cell(row, ColName).Value = line.Name;
            ws.Cell(row, ColSpec).Value = line.Spesifikasi ?? "";
            ws.Cell(row, ColVolume).Value = line.Volume;
            ws.Cell(row, ColUnit).Value = line.Unit;
            row++;
        }
        var lastRow = row - 1;

        if (lastRow >= 2)
        {
            ws.Range(1, ColId, lastRow, ColUnitPrice).Style.Protection.Locked = true;
            ws.Range(2, ColUnitPrice, lastRow, ColUnitPrice).Style.Protection.Locked = false;
        }

        ws.Column(ColId).Hide();
        ws.Columns().AdjustToContents();
        ws.Protect();

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
                var idCell = ws.Cell(row, ColId).GetString().Trim();
                var priceCell = ws.Cell(row, ColUnitPrice);

                // Baris kosong total (tidak ada ID maupun harga) dilewati diam-diam — bukan error,
                // ini cuma baris kosong sisa di bawah data (umum terjadi di Excel).
                if (idCell.Length == 0 && priceCell.IsEmpty()) continue;

                if (!Guid.TryParse(idCell, out var lineId))
                {
                    errors.Add($"Baris {row}: kolom ID kosong atau rusak — jangan ubah/hapus kolom ID di template.");
                    continue;
                }

                if (priceCell.IsEmpty() || !priceCell.TryGetValue<decimal>(out var unitPrice))
                {
                    errors.Add($"Baris {row}: Harga Satuan kosong atau bukan angka.");
                    continue;
                }

                if (unitPrice < 0)
                {
                    errors.Add($"Baris {row}: Harga Satuan tidak boleh negatif.");
                    continue;
                }

                lines.Add(new CreateVendorRabSubmissionLineRequest
                {
                    VendorRabRequestLineId = lineId,
                    UnitPrice = unitPrice,
                });
            }
        }

        if (errors.Count > 0) return (null, errors);
        if (lines.Count == 0) return (null, ["File tidak berisi baris harga apa pun."]);

        return (new CreateVendorRabSubmissionRequest { Lines = lines }, []);
    }
}
