using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.JournalEntry;
using SynteraERP.Api.DTOs.Reports;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class ReportsPdfService
{
    private readonly IReportsService _reportsService;
    private readonly AppDbContext _context;

    // Color palette — konsisten dengan InvoicePdfService/QuotationPdfService/SalesOrderPdfService
    private const string Navy = "#1E3A5F";
    private const string Blue = "#2563EB";
    private const string SlateGray = "#CBD5E1";
    private const string LightBlue = "#EFF6FF";
    private const string AltRow = "#F8FAFF";
    private const string Green = "#16A34A";
    private const string Red = "#DC2626";

    public ReportsPdfService(IReportsService reportsService, AppDbContext context)
    {
        _reportsService = reportsService;
        _context = context;
    }

    public async Task<byte[]> GenerateTrialBalanceAsync(DateOnly? asOfDate)
    {
        var rows = await _reportsService.GetTrialBalanceAsync(asOfDate);
        var effectiveDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var company = await GetCompanyAsync();

        var totalDebit = rows.Sum(x => x.TotalDebit);
        var totalCredit = rows.Sum(x => x.TotalCredit);

        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9));

                page.Header().Element(c => RenderReportHeader(c, company, "TRIAL BALANCE",
                    $"Per Tanggal {effectiveDate:dd/MM/yyyy}"));

                page.Content().Element(c => c.PaddingTop(12).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(70);
                            cols.RelativeColumn(3);
                            cols.ConstantColumn(90);
                            cols.ConstantColumn(90);
                        });

                        table.Header(h =>
                        {
                            static IContainer HC(IContainer c) => c.Background(Navy).Padding(4).AlignMiddle();
                            h.Cell().Element(HC).Text("Kode Akun").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Element(HC).Text("Nama Akun").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Element(HC).AlignRight().Text("Debit").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Element(HC).AlignRight().Text("Kredit").FontSize(8).Bold().FontColor(Colors.White);
                        });

                        for (int i = 0; i < rows.Count; i++)
                        {
                            var r = rows[i];
                            string bg = i % 2 == 1 ? AltRow : (string)Colors.White;
                            IContainer DC(IContainer c) => c.Background(bg).BorderBottom(0.5f).BorderColor(SlateGray).Padding(4).AlignMiddle();

                            table.Cell().Element(DC).Text(r.AccountCode).FontSize(8);
                            table.Cell().Element(DC).Text(r.AccountName).FontSize(8);
                            table.Cell().Element(DC).AlignRight().Text(FormatRupiah(r.TotalDebit)).FontSize(8);
                            table.Cell().Element(DC).AlignRight().Text(FormatRupiah(r.TotalCredit)).FontSize(8);
                        }

                        table.Footer(f =>
                        {
                            static IContainer FC(IContainer c) => c.Background(LightBlue).BorderTop(1).BorderColor(Navy).Padding(4).AlignMiddle();
                            f.Cell().Element(FC);
                            f.Cell().Element(FC).Text("TOTAL").FontSize(8).Bold().FontColor(Navy);
                            f.Cell().Element(FC).AlignRight().Text(FormatRupiah(totalDebit)).FontSize(8).Bold().FontColor(Navy);
                            f.Cell().Element(FC).AlignRight().Text(FormatRupiah(totalCredit)).FontSize(8).Bold().FontColor(Navy);
                        });
                    });

                    var isBalanced = totalDebit == totalCredit;
                    col.Item().PaddingTop(8).Text(isBalanced ? "Balance: Debit = Kredit" : $"TIDAK BALANCE — selisih {FormatRupiah(totalDebit - totalCredit)}")
                        .FontSize(8).Bold().FontColor(isBalanced ? Green : Red);
                }));

                page.Footer().Element(c => RenderFooter(c, company.CompanyName));
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> GenerateIncomeStatementAsync(DateOnly? startDate, DateOnly? endDate)
    {
        var data = await _reportsService.GetIncomeStatementAsync(startDate, endDate);
        var company = await GetCompanyAsync();

        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9));

                page.Header().Element(c => RenderReportHeader(c, company, "LAPORAN LABA RUGI",
                    $"Periode {data.StartDate:dd/MM/yyyy} - {data.EndDate:dd/MM/yyyy}"));

                page.Content().Element(c => c.PaddingTop(12).Column(col =>
                {
                    col.Spacing(10);
                    RenderIncomeStatementSection(col, "PENDAPATAN", data.Revenues, data.TotalRevenue);
                    RenderIncomeStatementSection(col, "BEBAN (termasuk HPP)", data.Expenses, data.TotalExpense);

                    col.Item().PaddingTop(6).BorderTop(1.5f).BorderColor(Navy).PaddingTop(6).Row(row =>
                    {
                        row.RelativeItem().Text("LABA / RUGI BERSIH").FontSize(10).Bold().FontColor(Navy);
                        row.ConstantItem(120).AlignRight().Text(FormatRupiah(data.NetIncome))
                            .FontSize(10).Bold().FontColor(data.NetIncome >= 0 ? Green : Red);
                    });
                }));

                page.Footer().Element(c => RenderFooter(c, company.CompanyName));
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> GenerateBalanceSheetAsync(DateOnly? asOfDate)
    {
        var data = await _reportsService.GetBalanceSheetAsync(asOfDate);
        var company = await GetCompanyAsync();

        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9));

                page.Header().Element(c => RenderReportHeader(c, company, "NERACA (BALANCE SHEET)",
                    $"Per Tanggal {DateOnly.FromDateTime(data.AsOfDate.Date):dd/MM/yyyy}"));

                page.Content().Element(c => c.PaddingTop(12).Column(col =>
                {
                    col.Spacing(10);
                    RenderBalanceSheetSection(col, "ASET", data.Assets, data.TotalAssets);
                    RenderBalanceSheetSection(col, "LIABILITAS", data.Liabilities, data.TotalLiabilities);
                    RenderBalanceSheetSection(col, "EKUITAS", data.Equities, data.TotalEquities);

                    var isBalanced = data.Selisih == 0;
                    col.Item().PaddingTop(6).BorderTop(1.5f).BorderColor(Navy).PaddingTop(6).Column(sum =>
                    {
                        sum.Item().Row(row =>
                        {
                            row.RelativeItem().Text("TOTAL ASET").FontSize(9).Bold();
                            row.ConstantItem(120).AlignRight().Text(FormatRupiah(data.TotalAssets)).FontSize(9).Bold();
                        });
                        sum.Item().Row(row =>
                        {
                            row.RelativeItem().Text("TOTAL LIABILITAS + EKUITAS").FontSize(9).Bold();
                            row.ConstantItem(120).AlignRight().Text(FormatRupiah(data.TotalLiabilities + data.TotalEquities)).FontSize(9).Bold();
                        });
                        sum.Item().PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem().Text("SELISIH").FontSize(9).Bold().FontColor(isBalanced ? Green : Red);
                            row.ConstantItem(120).AlignRight().Text(FormatRupiah(data.Selisih))
                                .FontSize(9).Bold().FontColor(isBalanced ? Green : Red);
                        });
                    });
                }));

                page.Footer().Element(c => RenderFooter(c, company.CompanyName));
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]?> GenerateGeneralLedgerAsync(Guid accountId, DateOnly? startDate, DateOnly? endDate)
    {
        var data = await _reportsService.GetGeneralLedgerAsync(accountId, startDate, endDate);
        if (data is null) return null;

        var company = await GetCompanyAsync();

        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9));

                page.Header().Element(c => RenderReportHeader(c, company, "BUKU BESAR (GENERAL LEDGER)",
                    $"{data.AccountCode} - {data.AccountName} | Periode {data.StartDate:dd/MM/yyyy} - {data.EndDate:dd/MM/yyyy}"));

                page.Content().Element(c => c.PaddingTop(12).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Saldo Awal").FontSize(9).FontColor(Colors.Grey.Medium);
                        row.ConstantItem(120).AlignRight().Text(FormatRupiah(data.OpeningBalance)).FontSize(9).Bold();
                    });

                    col.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(60);
                            cols.ConstantColumn(70);
                            cols.RelativeColumn(3);
                            cols.ConstantColumn(75);
                            cols.ConstantColumn(75);
                            cols.ConstantColumn(85);
                        });

                        table.Header(h =>
                        {
                            static IContainer HC(IContainer c) => c.Background(Navy).Padding(4).AlignMiddle();
                            h.Cell().Element(HC).Text("Tanggal").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Element(HC).Text("No. Jurnal").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Element(HC).Text("Deskripsi").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Element(HC).AlignRight().Text("Debit").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Element(HC).AlignRight().Text("Kredit").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Element(HC).AlignRight().Text("Saldo").FontSize(8).Bold().FontColor(Colors.White);
                        });

                        for (int i = 0; i < data.Lines.Count; i++)
                        {
                            var l = data.Lines[i];
                            string bg = i % 2 == 1 ? AltRow : (string)Colors.White;
                            IContainer DC(IContainer c) => c.Background(bg).BorderBottom(0.5f).BorderColor(SlateGray).Padding(4).AlignMiddle();

                            table.Cell().Element(DC).Text(l.Date.ToString("dd/MM/yyyy")).FontSize(8);
                            table.Cell().Element(DC).Text(l.EntryNumber).FontSize(8);
                            table.Cell().Element(DC).Text(l.Description).FontSize(8);
                            table.Cell().Element(DC).AlignRight().Text(l.Debit == 0 ? "-" : FormatRupiah(l.Debit)).FontSize(8);
                            table.Cell().Element(DC).AlignRight().Text(l.Credit == 0 ? "-" : FormatRupiah(l.Credit)).FontSize(8);
                            table.Cell().Element(DC).AlignRight().Text(FormatRupiah(l.RunningBalance)).FontSize(8).Bold();
                        }
                    });

                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Text("Saldo Akhir").FontSize(9).Bold();
                        row.ConstantItem(120).AlignRight().Text(FormatRupiah(data.ClosingBalance)).FontSize(9).Bold().FontColor(Navy);
                    });
                }));

                page.Footer().Element(c => RenderFooter(c, company.CompanyName));
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> GeneratePpnReconciliationAsync(DateOnly? startDate, DateOnly? endDate)
    {
        var data = await _reportsService.GetPpnReconciliationAsync(startDate, endDate);
        var company = await GetCompanyAsync();

        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9));

                page.Header().Element(c => RenderReportHeader(c, company, "REKAPITULASI PPN",
                    $"Periode {data.StartDate:dd/MM/yyyy} - {data.EndDate:dd/MM/yyyy}"));

                page.Content().Element(c => c.PaddingTop(12).Column(col =>
                {
                    col.Spacing(10);
                    RenderPpnSection(col, "PPN KELUARAN (dari Invoice AR)", data.PpnKeluaran, data.TotalPpnKeluaran);
                    RenderPpnSection(col, "PPN MASUKAN (dari Supplier Invoice)", data.PpnMasukan, data.TotalPpnMasukan);

                    col.Item().PaddingTop(6).BorderTop(1.5f).BorderColor(Navy).PaddingTop(6).Row(row =>
                    {
                        row.RelativeItem().Text("SELISIH (PPN Keluaran - PPN Masukan)").FontSize(10).Bold().FontColor(Navy);
                        row.ConstantItem(120).AlignRight().Text(FormatRupiah(data.Selisih))
                            .FontSize(10).Bold().FontColor(data.Selisih >= 0 ? Green : Red);
                    });
                    col.Item().Text(data.Selisih >= 0
                        ? "Selisih positif: PPN Kurang Bayar (harus disetor)."
                        : "Selisih negatif: PPN Lebih Bayar (bisa dikompensasi/restitusi).")
                        .FontSize(8).FontColor(Colors.Grey.Medium);
                }));

                page.Footer().Element(c => RenderFooter(c, company.CompanyName));
            });
        });

        return document.GeneratePdf();
    }

    // ── Shared rendering ──────────────────────────────────────────────────────

    private static void RenderReportHeader(IContainer container, CompanySettings company, string title, string subtitle)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(info =>
                {
                    info.Item().Text(company.CompanyName).Bold().FontSize(11).FontColor(Colors.Grey.Darken3);
                    if (!string.IsNullOrEmpty(company.Address))
                        info.Item().Text(company.Address).FontSize(8).FontColor(Colors.Grey.Medium);
                });

                row.ConstantItem(220).Column(t =>
                {
                    t.Item().AlignRight().Text(title).Bold().FontSize(16).FontColor(Navy);
                    t.Item().AlignRight().Text(subtitle).FontSize(8).FontColor(Colors.Grey.Darken2);
                });
            });

            col.Item().PaddingTop(8).BorderBottom(2).BorderColor(Blue).Height(2);
        });
    }

    private static void RenderFooter(IContainer container, string? companyName)
    {
        var footerText = string.IsNullOrWhiteSpace(companyName)
            ? "Dokumen dicetak otomatis oleh sistem."
            : $"Dokumen dicetak otomatis oleh sistem {companyName}.";

        container.Column(col =>
        {
            col.Item().BorderTop(0.5f).BorderColor(SlateGray).PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text(footerText)
                    .FontSize(7).FontColor(Colors.Grey.Medium);

                row.ConstantItem(80).AlignRight().Text(text =>
                {
                    text.Span("Halaman ").FontSize(7).FontColor(Colors.Grey.Medium);
                    text.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                    text.Span(" dari ").FontSize(7).FontColor(Colors.Grey.Medium);
                    text.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                });
            });
        });
    }

    private static void RenderIncomeStatementSection(ColumnDescriptor col, string title, List<IncomeStatementAccountRowDto> rows, decimal total)
    {
        col.Item().Column(section =>
        {
            section.Item().Text(title).FontSize(9).Bold().FontColor(Navy);

            section.Item().PaddingTop(2).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(70);
                    cols.RelativeColumn();
                    cols.ConstantColumn(100);
                });

                for (int i = 0; i < rows.Count; i++)
                {
                    var r = rows[i];
                    string bg = i % 2 == 1 ? AltRow : (string)Colors.White;
                    IContainer DC(IContainer c) => c.Background(bg).BorderBottom(0.5f).BorderColor(SlateGray).Padding(3).AlignMiddle();

                    table.Cell().Element(DC).Text(r.AccountCode).FontSize(8);
                    table.Cell().Element(DC).Text(r.AccountName).FontSize(8);
                    table.Cell().Element(DC).AlignRight().Text(FormatRupiah(r.Amount)).FontSize(8);
                }
            });

            section.Item().PaddingTop(2).BorderTop(0.5f).BorderColor(SlateGray).PaddingTop(2).Row(row =>
            {
                row.RelativeItem().Text($"Total {title}").FontSize(8).Bold();
                row.ConstantItem(100).AlignRight().Text(FormatRupiah(total)).FontSize(8).Bold();
            });
        });
    }

    private static void RenderBalanceSheetSection(ColumnDescriptor col, string title, List<BalanceSheetAccountRowDto> rows, decimal total)
    {
        col.Item().Column(section =>
        {
            section.Item().Background(LightBlue).Padding(3).Text(title).FontSize(9).Bold().FontColor(Navy);

            section.Item().PaddingTop(2).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(70);
                    cols.RelativeColumn();
                    cols.ConstantColumn(100);
                });

                for (int i = 0; i < rows.Count; i++)
                {
                    var r = rows[i];
                    string bg = i % 2 == 1 ? AltRow : (string)Colors.White;
                    IContainer DC(IContainer c) => c.Background(bg).BorderBottom(0.5f).BorderColor(SlateGray).Padding(3).AlignMiddle();

                    table.Cell().Element(DC).Text(r.AccountCode).FontSize(8);
                    table.Cell().Element(DC).Text(r.AccountName).FontSize(8);
                    table.Cell().Element(DC).AlignRight().Text(FormatRupiah(r.Balance)).FontSize(8);
                }
            });

            section.Item().PaddingTop(2).BorderTop(0.5f).BorderColor(SlateGray).PaddingTop(2).Row(row =>
            {
                row.RelativeItem().Text($"Total {title}").FontSize(8).Bold();
                row.ConstantItem(100).AlignRight().Text(FormatRupiah(total)).FontSize(8).Bold();
            });
        });
    }

    private static void RenderPpnSection(ColumnDescriptor col, string title, List<PpnReconciliationRowDto> rows, decimal total)
    {
        col.Item().Column(section =>
        {
            section.Item().Background(LightBlue).Padding(3).Text(title).FontSize(9).Bold().FontColor(Navy);

            section.Item().PaddingTop(2).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(55);
                    cols.ConstantColumn(75);
                    cols.RelativeColumn(2);
                    cols.RelativeColumn();
                    cols.RelativeColumn();
                    cols.ConstantColumn(85);
                });

                table.Header(h =>
                {
                    static IContainer HC(IContainer c) => c.BorderBottom(1).BorderColor(SlateGray).Padding(3).AlignMiddle();
                    h.Cell().Element(HC).Text("Tanggal").FontSize(7).Bold();
                    h.Cell().Element(HC).Text("No. Dokumen").FontSize(7).Bold();
                    h.Cell().Element(HC).Text("Customer/Supplier").FontSize(7).Bold();
                    h.Cell().Element(HC).Text("NPWP").FontSize(7).Bold();
                    h.Cell().Element(HC).Text("No. Faktur Pajak").FontSize(7).Bold();
                    h.Cell().Element(HC).AlignRight().Text("Jumlah PPN").FontSize(7).Bold();
                });

                for (int i = 0; i < rows.Count; i++)
                {
                    var r = rows[i];
                    string bg = i % 2 == 1 ? AltRow : (string)Colors.White;
                    IContainer DC(IContainer c) => c.Background(bg).BorderBottom(0.5f).BorderColor(SlateGray).Padding(3).AlignMiddle();

                    table.Cell().Element(DC).Text(r.Date.ToString("dd/MM/yyyy")).FontSize(7);
                    table.Cell().Element(DC).Text(r.DocumentNo).FontSize(7);
                    table.Cell().Element(DC).Text(r.PartnerName ?? "-").FontSize(7);
                    table.Cell().Element(DC).Text(r.Npwp ?? "-").FontSize(7);
                    table.Cell().Element(DC).Text(r.NomorFakturPajak ?? "-").FontSize(7);
                    table.Cell().Element(DC).AlignRight().Text(FormatRupiah(r.Amount)).FontSize(7);
                }
            });

            section.Item().PaddingTop(2).BorderTop(0.5f).BorderColor(SlateGray).PaddingTop(2).Row(row =>
            {
                row.RelativeItem().Text($"Total {title}").FontSize(8).Bold();
                row.ConstantItem(100).AlignRight().Text(FormatRupiah(total)).FontSize(8).Bold();
            });
        });
    }

    private async Task<CompanySettings> GetCompanyAsync() =>
        await _context.CompanySettings.FirstOrDefaultAsync()
            ?? new CompanySettings { CompanyName = "Perusahaan Anda" };

    private static string FormatRupiah(decimal value) =>
        $"Rp {value:N0}".Replace(",", ".");
}
