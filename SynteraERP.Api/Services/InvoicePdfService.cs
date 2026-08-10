using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SynteraERP.Api.Data;
using SynteraERP.Api.Models;

namespace SynteraERP.Api.Services;

public class InvoicePdfService
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    // Color palette
    private const string Navy      = "#1E3A5F";
    private const string Blue      = "#2563EB";
    private const string SlateGray = "#CBD5E1";
    private const string LightBlue = "#EFF6FF";
    private const string AltRow    = "#F8FAFF";
    private const string Green     = "#16A34A";
    private const string Red       = "#DC2626";

    public InvoicePdfService(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    public async Task<byte[]?> GenerateAsync(Guid invoiceId)
    {
        var invoice = await _context.Invoices
            .Include(x => x.Customer)
            .Include(x => x.SalesOrder)
                .ThenInclude(so => so!.Items.OrderBy(i => i.SortOrder))
            .Include(x => x.Items.OrderBy(i => i.SortOrder))
            .Include(x => x.Payments.OrderBy(p => p.PaymentDate))
            .FirstOrDefaultAsync(x => x.Id == invoiceId && !x.IsDeleted);

        if (invoice is null) return null;

        var company = await _context.CompanySettings.FirstOrDefaultAsync()
            ?? new CompanySettings { CompanyName = "Perusahaan Anda" };

        byte[]? logoBytes = null;
        if (!string.IsNullOrEmpty(company.LogoPath))
        {
            var logoPath = Path.Combine(_env.ContentRootPath, "uploads", company.LogoPath);
            if (File.Exists(logoPath))
                logoBytes = await File.ReadAllBytesAsync(logoPath);
        }

        var hasInvItems = invoice.Items != null && invoice.Items.Any();
        decimal subTotal  = hasInvItems
            ? Math.Round(invoice.Items!.Sum(x => x.Amount), 2)
            : Math.Round(invoice.Amount / 1.11m, 2);
        decimal taxAmount = hasInvItems
            ? Math.Round(subTotal * 0.11m, 2)
            : invoice.Amount - subTotal;
        decimal balance   = invoice.Amount - invoice.Paid;

        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9));

                page.Header().Element(c => RenderHeader(c, invoice, company, logoBytes));
                page.Content().Element(c =>
                    RenderContent(c, invoice, company, subTotal, taxAmount, balance));
                page.Footer().Element(c => RenderFooter(c, company.CompanyName));
            });
        });

        return document.GeneratePdf();
    }

    // ── Header ────────────────────────────────────────────────────────────────

    private static void RenderHeader(
        IContainer container,
        Invoice invoice,
        CompanySettings company,
        byte[]? logoBytes)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                // Kiri: Logo + Company
                row.RelativeItem().Row(logoRow =>
                {
                    if (logoBytes is not null)
                    {
                        logoRow.ConstantItem(60).Height(60).Image(logoBytes).FitArea();
                        logoRow.ConstantItem(10);
                    }

                    logoRow.RelativeItem().Column(info =>
                    {
                        info.Item().Text(company.CompanyName)
                            .Bold().FontSize(11).FontColor(Colors.Grey.Darken3);

                        if (!string.IsNullOrEmpty(company.Address))
                            info.Item().Text(company.Address)
                                .FontSize(8).FontColor(Colors.Grey.Medium);

                        if (!string.IsNullOrEmpty(company.Phone))
                            info.Item().Text($"Telp: {company.Phone}")
                                .FontSize(8).FontColor(Colors.Grey.Medium);

                        if (!string.IsNullOrEmpty(company.Email))
                            info.Item().Text(company.Email)
                                .FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });

                // Kanan: Judul INVOICE
                row.ConstantItem(180).Column(title =>
                {
                    title.Item().AlignRight()
                        .Text("INVOICE")
                        .Bold().FontSize(28).FontColor(Navy);

                    title.Item().AlignRight()
                        .Text(invoice.No)
                        .Bold().FontSize(10).FontColor(Navy);

                    // Badge status jika belum Paid
                    if (invoice.Status != InvoiceStatus.Paid)
                    {
                        var statusColor = invoice.Status == InvoiceStatus.Overdue ? Red : Blue;
                        var statusText  = invoice.Status == InvoiceStatus.Overdue
                            ? "OVERDUE"
                            : invoice.Status.ToString().ToUpper();

                        title.Item().AlignRight().PaddingTop(4)
                            .Background(statusColor).Padding(4)
                            .Text(statusText).FontSize(8).Bold().FontColor(Colors.White);
                    }
                });
            });

            col.Item().PaddingTop(8).BorderBottom(2).BorderColor(Blue).Height(2);
        });
    }

    // ── Content ───────────────────────────────────────────────────────────────

    private static void RenderContent(
        IContainer container,
        Invoice invoice,
        CompanySettings company,
        decimal subTotal,
        decimal taxAmount,
        decimal balance)
    {
        container.Column(col =>
        {
            col.Spacing(12);

            // ── Info Customer + Meta ──
            col.Item().Row(row =>
            {
                // Kiri: Kepada
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text("Kepada:")
                        .FontSize(8).FontColor(Colors.Grey.Medium).Italic();
                    left.Item().Text(invoice.Customer.Name)
                        .Bold().FontSize(10);

                    if (!string.IsNullOrEmpty(invoice.Customer.Address))
                        left.Item().Text(invoice.Customer.Address)
                            .FontSize(9).FontColor(Colors.Grey.Darken2);

                    if (!string.IsNullOrEmpty(invoice.Customer.Npwp))
                        left.Item().Text($"NPWP: {invoice.Customer.Npwp}")
                            .FontSize(8).FontColor(Colors.Grey.Medium);
                });

                // Kanan: Meta
                row.ConstantItem(220).Column(right =>
                {
                    void MetaRow(string label, string value) =>
                        right.Item().Row(r =>
                        {
                            r.ConstantItem(90).Text(label)
                                .FontSize(9).FontColor(Colors.Grey.Medium);
                            r.RelativeItem().Text(value).FontSize(9).Bold();
                        });

                    MetaRow("Invoice Date", invoice.InvoiceDate.ToString("dd/MM/yyyy"));
                    MetaRow("Due Date",     invoice.DueDate.ToString("dd/MM/yyyy"));
                    MetaRow("Terms",        invoice.Terms ?? "—");

                    if (invoice.SalesOrder is not null)
                        MetaRow("Ref SO", invoice.SalesOrder.No);
                });
            });

            // ── Tabel Items ──
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(22);   // #
                    cols.RelativeColumn(5);    // Deskripsi
                    cols.ConstantColumn(44);   // Qty
                    cols.ConstantColumn(44);   // UoM
                    cols.ConstantColumn(80);   // Harga Satuan
                    cols.ConstantColumn(80);   // Total
                });

                // Header
                table.Header(h =>
                {
                    static IContainer HC(IContainer c) => c
                        .Background(Navy).Padding(4).AlignMiddle();

                    h.Cell().Element(HC).Text("#")
                        .FontSize(8).Bold().FontColor(Colors.White);
                    h.Cell().Element(HC).Text("Deskripsi")
                        .FontSize(8).Bold().FontColor(Colors.White);
                    h.Cell().Element(HC).AlignRight().Text("Qty")
                        .FontSize(8).Bold().FontColor(Colors.White);
                    h.Cell().Element(HC).Text("UoM")
                        .FontSize(8).Bold().FontColor(Colors.White);
                    h.Cell().Element(HC).AlignRight().Text("Harga Satuan")
                        .FontSize(8).Bold().FontColor(Colors.White);
                    h.Cell().Element(HC).AlignRight().Text("Total")
                        .FontSize(8).Bold().FontColor(Colors.White);
                });

                // Data rows — prioritas: InvoiceItems > SOItems > fallback
                var useInvoiceItems = invoice.Items != null && invoice.Items.Any();
                var soItems         = invoice.SalesOrder?.Items?.OrderBy(x => x.SortOrder).ToList();
                var useSoItems      = !useInvoiceItems && soItems != null && soItems.Count > 0;

                if (useInvoiceItems)
                {
                    var invItems = invoice.Items!.OrderBy(x => x.SortOrder).ToList();
                    for (int i = 0; i < invItems.Count; i++)
                    {
                        var item = invItems[i];
                        string bg = i % 2 == 1 ? AltRow : (string)Colors.White;

                        IContainer DC(IContainer c) => c
                            .Background(bg).BorderBottom(0.5f)
                            .BorderColor(SlateGray).Padding(4).AlignMiddle();

                        table.Cell().Element(DC).Text($"{i + 1}").FontSize(8);
                        table.Cell().Element(DC).Column(dc =>
                        {
                            dc.Item().Text(item.Description).FontSize(8).Bold();
                            if (!string.IsNullOrEmpty(item.Sku))
                                dc.Item().Text($"SKU: {item.Sku}")
                                    .FontSize(7).FontColor(Colors.Grey.Medium);
                        });
                        table.Cell().Element(DC).AlignRight()
                            .Text(FormatQty(item.Qty)).FontSize(8);
                        table.Cell().Element(DC).Text(item.Uom).FontSize(8);
                        table.Cell().Element(DC).AlignRight()
                            .Text(FormatRupiah(item.UnitPrice)).FontSize(8);
                        table.Cell().Element(DC).AlignRight()
                            .Text(FormatRupiah(item.Amount)).FontSize(8);
                    }
                }
                else if (useSoItems)
                {
                    for (int i = 0; i < soItems!.Count; i++)
                    {
                        var item = soItems[i];
                        string bg = i % 2 == 1 ? AltRow : (string)Colors.White;

                        IContainer DC(IContainer c) => c
                            .Background(bg).BorderBottom(0.5f)
                            .BorderColor(SlateGray).Padding(4).AlignMiddle();

                        table.Cell().Element(DC).Text($"{i + 1}").FontSize(8);
                        table.Cell().Element(DC).Column(dc =>
                        {
                            dc.Item().Text(item.Description).FontSize(8).Bold();
                            if (!string.IsNullOrEmpty(item.Sku))
                                dc.Item().Text($"SKU: {item.Sku}")
                                    .FontSize(7).FontColor(Colors.Grey.Medium);
                        });
                        table.Cell().Element(DC).AlignRight()
                            .Text(FormatQty(item.Qty)).FontSize(8);
                        table.Cell().Element(DC).Text(item.Uom).FontSize(8);
                        table.Cell().Element(DC).AlignRight()
                            .Text(FormatRupiah(item.UnitPrice)).FontSize(8);
                        table.Cell().Element(DC).AlignRight()
                            .Text(FormatRupiah(item.Amount)).FontSize(8);
                    }
                }
                else
                {
                    // Fallback: single summary row
                    IContainer DC(IContainer c) => c
                        .Background(Colors.White).BorderBottom(0.5f)
                        .BorderColor(SlateGray).Padding(4).AlignMiddle();

                    table.Cell().Element(DC).Text("1").FontSize(8);
                    table.Cell().Element(DC)
                        .Text("Jasa dan Material sesuai Sales Order").FontSize(8);
                    table.Cell().Element(DC).AlignRight().Text("1").FontSize(8);
                    table.Cell().Element(DC).Text("paket").FontSize(8);
                    table.Cell().Element(DC).AlignRight()
                        .Text(FormatRupiah(subTotal)).FontSize(8);
                    table.Cell().Element(DC).AlignRight()
                        .Text(FormatRupiah(subTotal)).FontSize(8);
                }
            });

            // ── Payment Summary ──
            col.Item().AlignRight().Width(280).Column(summary =>
            {
                summary.Spacing(3);

                SummaryRow(summary, "Sub Total", FormatRupiah(subTotal));
                SummaryRow(summary, "PPN (11%)", FormatRupiah(taxAmount));
                SummaryRow(summary, "Grand Total", FormatRupiah(invoice.Amount),
                    bold: true, topBorder: true);
                SummaryRow(summary, "Terbayar", FormatRupiah(invoice.Paid),
                    color: Green);
                SummaryRow(summary, "Sisa Tagihan", FormatRupiah(balance),
                    bold: balance > 0, topBorder: true,
                    color: balance > 0 ? Red : Green);
            });

            // ── Riwayat Pembayaran ──
            if (invoice.Payments.Any())
            {
                col.Item().Column(ph =>
                {
                    ph.Item().PaddingBottom(4)
                        .Text("Riwayat Pembayaran")
                        .FontSize(9).Bold().FontColor(Navy);

                    ph.Item().Table(pt =>
                    {
                        pt.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(80);
                            c.ConstantColumn(70);
                            c.RelativeColumn();
                            c.ConstantColumn(90);
                        });

                        pt.Header(h =>
                        {
                            static IContainer PH(IContainer c) => c
                                .Background(LightBlue).Padding(4);

                            h.Cell().Element(PH).Text("Tanggal")
                                .FontSize(8).Bold().FontColor(Navy);
                            h.Cell().Element(PH).Text("Metode")
                                .FontSize(8).Bold().FontColor(Navy);
                            h.Cell().Element(PH).Text("Referensi")
                                .FontSize(8).Bold().FontColor(Navy);
                            h.Cell().Element(PH).AlignRight().Text("Jumlah")
                                .FontSize(8).Bold().FontColor(Navy);
                        });

                        foreach (var payment in invoice.Payments)
                        {
                            static IContainer PC(IContainer c) => c
                                .BorderBottom(0.5f).BorderColor(SlateGray).Padding(4);

                            pt.Cell().Element(PC)
                                .Text(payment.PaymentDate.ToString("dd/MM/yyyy"))
                                .FontSize(8);
                            pt.Cell().Element(PC)
                                .Text(payment.Method.ToString()).FontSize(8);
                            pt.Cell().Element(PC)
                                .Text(payment.Reference ?? "—")
                                .FontSize(8).FontColor(Colors.Grey.Medium);
                            pt.Cell().Element(PC).AlignRight()
                                .Text(FormatRupiah(payment.Amount))
                                .FontSize(8).FontColor(Green);
                        }
                    });
                });
            }

            // ── Info Pembayaran (dari FooterText) ──
            if (!string.IsNullOrEmpty(company.FooterText))
            {
                col.Item()
                    .Background(LightBlue).Border(0.5f).BorderColor("#DBEAFE").Padding(8)
                    .Column(bank =>
                    {
                        bank.Item().Text("Informasi Pembayaran")
                            .FontSize(8).Bold().FontColor(Navy);
                        bank.Item().PaddingTop(2).Text(company.FooterText)
                            .FontSize(8).FontColor(Colors.Grey.Darken2);
                    });
            }

            // ── Tanda Tangan ──
            col.Item().PaddingTop(20).Row(row =>
            {
                row.RelativeItem();
                row.ConstantItem(160).Column(ttd =>
                {
                    ttd.Item().AlignCenter().Text("Hormat kami,")
                        .FontSize(8).FontColor(Colors.Grey.Medium);
                    ttd.Item().Height(40);
                    ttd.Item().BorderBottom(0.5f).BorderColor(SlateGray).Height(1);
                    ttd.Item().AlignCenter().PaddingTop(4)
                        .Text(company.SignatureName ?? company.CompanyName)
                        .FontSize(8).Bold();

                    if (!string.IsNullOrEmpty(company.SignatureTitle))
                        ttd.Item().AlignCenter()
                            .Text(company.SignatureTitle)
                            .FontSize(7).FontColor(Colors.Grey.Medium);
                });
            });
        });
    }

    // ── Footer ────────────────────────────────────────────────────────────────

    private static void RenderFooter(IContainer container, string? companyName)
    {
        var footerText = string.IsNullOrWhiteSpace(companyName)
            ? "Dokumen dicetak otomatis oleh sistem."
            : $"Dokumen dicetak otomatis oleh sistem {companyName}.";

        container.Column(col =>
        {
            col.Item().BorderTop(0.5f).BorderColor(SlateGray).PaddingTop(4).Row(row =>
            {
                row.RelativeItem()
                    .Text(footerText)
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

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void SummaryRow(
        ColumnDescriptor col,
        string label,
        string value,
        bool bold = false,
        bool topBorder = false,
        string? color = null)
    {
        var item = col.Item();
        if (topBorder)
            item = item.BorderTop(0.5f).BorderColor(SlateGray).PaddingTop(4);

        item.Row(r =>
        {
            string fgColor = color ?? (string)Colors.Grey.Darken2;
            var fontSize = bold ? 10 : 9;

            var lt = r.RelativeItem().Text(label).FontSize(fontSize).FontColor(fgColor);
            if (bold) lt.Bold();

            var vt = r.ConstantItem(100).AlignRight().Text(value).FontSize(fontSize).FontColor(fgColor);
            if (bold) vt.Bold();
        });
    }

    private static string FormatRupiah(decimal value) =>
        $"Rp {value:N0}".Replace(",", ".");

    private static string FormatQty(decimal value) =>
        value == Math.Floor(value)
            ? ((int)value).ToString()
            : value.ToString("G29");
}
