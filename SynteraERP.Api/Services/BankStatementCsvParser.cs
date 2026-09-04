using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using SynteraERP.Api.DTOs.BankReconciliation;

namespace SynteraERP.Api.Services;

public record ParsedBankStatementRow(DateOnly TransactionDate, string Description, decimal Amount);

// Parser murni (tanpa DB dependency) - format tetap: Tanggal,Keterangan,Debit,Kredit
// (header wajib, dicocokkan by name lewat CsvHelper, bukan posisi kolom). Kumpulkan SEMUA
// baris error (bukan berhenti di error pertama) supaya caller bisa reject 1 file sekaligus
// dengan daftar lengkap baris+alasan, bukan partial-import.
public static class BankStatementCsvParser
{
    private static readonly string[] RequiredHeaders = ["Tanggal", "Keterangan", "Debit", "Kredit"];

    public static (List<ParsedBankStatementRow> Rows, List<CsvRowError> Errors) Parse(Stream csvStream)
    {
        var rows = new List<ParsedBankStatementRow>();
        var errors = new List<CsvRowError>();

        using var reader = new StreamReader(csvStream);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
        };
        using var csv = new CsvReader(reader, config);

        if (!csv.Read() || !csv.ReadHeader())
        {
            errors.Add(new CsvRowError { RowNumber = 1, Reason = "File CSV kosong atau tidak punya baris header." });
            return (rows, errors);
        }

        var headerRecord = csv.HeaderRecord ?? [];
        var missingHeaders = RequiredHeaders
            .Where(h => !headerRecord.Any(hr => string.Equals(hr.Trim(), h, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (missingHeaders.Count > 0)
        {
            errors.Add(new CsvRowError
            {
                RowNumber = 1,
                Reason = $"Header CSV harus punya kolom Tanggal, Keterangan, Debit, Kredit. Kolom hilang: {string.Join(", ", missingHeaders)}.",
            });
            return (rows, errors);
        }

        while (csv.Read())
        {
            var rowNumber = csv.Context.Parser?.Row ?? 0;
            var tanggalRaw = csv.GetField("Tanggal")?.Trim() ?? "";
            var keteranganRaw = csv.GetField("Keterangan")?.Trim() ?? "";
            var debitRaw = csv.GetField("Debit")?.Trim() ?? "";
            var kreditRaw = csv.GetField("Kredit")?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(tanggalRaw) && string.IsNullOrWhiteSpace(keteranganRaw) &&
                string.IsNullOrWhiteSpace(debitRaw) && string.IsNullOrWhiteSpace(kreditRaw))
            {
                continue; // baris kosong (mis. baris terakhir file) - dilewati, bukan error
            }

            if (!DateOnly.TryParseExact(tanggalRaw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var tanggal))
            {
                errors.Add(new CsvRowError { RowNumber = rowNumber, Reason = $"Format tanggal tidak valid ('{tanggalRaw}'). Format yang benar: yyyy-MM-dd." });
                continue;
            }

            if (string.IsNullOrWhiteSpace(keteranganRaw))
            {
                errors.Add(new CsvRowError { RowNumber = rowNumber, Reason = "Keterangan tidak boleh kosong." });
                continue;
            }

            var hasDebit = !string.IsNullOrWhiteSpace(debitRaw);
            var hasKredit = !string.IsNullOrWhiteSpace(kreditRaw);

            if (hasDebit && hasKredit)
            {
                errors.Add(new CsvRowError { RowNumber = rowNumber, Reason = "Debit dan Kredit tidak boleh dua-duanya terisi." });
                continue;
            }
            if (!hasDebit && !hasKredit)
            {
                errors.Add(new CsvRowError { RowNumber = rowNumber, Reason = "Debit dan Kredit tidak boleh dua-duanya kosong." });
                continue;
            }

            decimal amount;
            if (hasDebit)
            {
                if (!decimal.TryParse(debitRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var debitVal) || debitVal <= 0)
                {
                    errors.Add(new CsvRowError { RowNumber = rowNumber, Reason = $"Nilai Debit tidak valid ('{debitRaw}')." });
                    continue;
                }
                amount = debitVal;
            }
            else
            {
                if (!decimal.TryParse(kreditRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var kreditVal) || kreditVal <= 0)
                {
                    errors.Add(new CsvRowError { RowNumber = rowNumber, Reason = $"Nilai Kredit tidak valid ('{kreditRaw}')." });
                    continue;
                }
                amount = -kreditVal;
            }

            rows.Add(new ParsedBankStatementRow(tanggal, keteranganRaw, amount));
        }

        return (rows, errors);
    }
}
