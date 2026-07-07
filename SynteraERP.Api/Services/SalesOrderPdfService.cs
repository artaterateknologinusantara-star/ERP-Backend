using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SynteraERP.Api.Data;
using SynteraERP.Api.Models;

namespace SynteraERP.Api.Services;

public class SalesOrderPdfService
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    // Color palette
    private const string Navy      = "#1E3A5F";
    private const string Blue      = "#2563EB";
    private const string SlateGray = "#CBD5E1";
    private const string LightBlue = "#EFF6FF";
    private const string AltRow    = "#F8FAFF";

    public SalesOrderPdfService(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    public async Task<byte[]?> GenerateAsync(Guid salesOrderId)
    {
        var so = await _context.SalesOrders
            .Include(x => x.Customer)
            .Include(x => x.Sales)
            .Include(x => x.Items.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(x => x.Id == salesOrderId && !x.IsDeleted);

        if (so is null) return null;

        var company = await _context.CompanySettings.FirstOrDefaultAsync()
            ?? new CompanySettings { CompanyName = "PT Syntera Teknologi Nusantara" };

        byte[]? logoBytes = null;
        if (!string.IsNullOrEmpty(company.LogoPath))
        {
            var logoPath = Path.Combine(_env.ContentRootPath, "uploads", company.LogoPath);
            if (File.Exists(logoPath))
                logoBytes = await File.ReadAllBytesAsync(logoPath);
        }

        decimal subTotal = so.Items.Any()
            ? so.Items.Sum(x => x.Amount)
            : Math.Round(so.Total / 1.11m, 2);
        decimal taxAmount = Math.Round(subTotal * 11 / 100, 2);
        decimal grandTotal = subTotal + taxAmount;

        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9));

                page.Header().Element(c => RenderHeader(c, so, company, logoBytes));
                page.Content().Element(c => RenderContent(c, so, company, subTotal, taxAmount, grandTotal));
                page.Footer().Element(c => RenderFooter(c));
            });
        });

        return document.GeneratePdf();
    }

    // ── Header ────────────────────────────────────────────────────────────────

    private static void RenderHeader(
        IContainer container,
        SalesOrder so,
        CompanySettings company,
        byte[]? logoBytes)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                // Kiri: Logo + Company Info
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

                // Kanan: Judul SO
                row.ConstantItem(180).Column(title =>
                {
                    title.Item().AlignRight()
                        .Text("SO")
                        .Bold().FontSize(32).FontColor(Navy);

                    title.Item().AlignRight()
                        .Text($"Sales Order# {so.No}")
                        .Bold().FontSize(10).FontColor(Navy);
                });
            });

            col.Item().PaddingTop(8).BorderBottom(2).BorderColor(Blue).Height(2);
        });
    }

    // ── Content ───────────────────────────────────────────────────────────────

    private static void RenderContent(
        IContainer container,
        SalesOrder so,
        CompanySettings company,
        decimal subTotal,
        decimal taxAmount,
        decimal grandTotal)
    {
        container.Column(col =>
        {
            col.Spacing(12);

            // ── Ship To + Meta Info ──
            col.Item().Row(row =>
            {
                // Kiri: Ship To
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text("Ship To")
                        .FontSize(8).FontColor(Colors.Grey.Medium).Italic();
                    left.Item().Text(so.Customer.Name)
                        .Bold().FontSize(10);

                    var shipAddress = !string.IsNullOrEmpty(so.ShipTo)
                        ? so.ShipTo
                        : so.Customer.Address;

                    if (!string.IsNullOrEmpty(shipAddress))
                    {
                        foreach (var line in shipAddress.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                            left.Item().Text(line.Trim())
                                .FontSize(9).FontColor(Colors.Grey.Darken2);
                    }
                });

                // Kanan: Meta
                row.ConstantItem(220).Column(right =>
                {
                    void MetaRow(string label, string value) =>
                        right.Item().Row(r =>
                        {
                            r.ConstantItem(90).Text(label)
                                .FontSize(9).FontColor(Colors.Grey.Medium);
                            r.RelativeItem().Text(value)
                                .FontSize(9).Bold();
                        });

                    MetaRow("Order Date", so.Date.ToString("dd/MM/yyyy"));
                    MetaRow("Terms", so.Terms ?? "—");
                    MetaRow("Expected Date", so.ExpectedDate.HasValue
                        ? so.ExpectedDate.Value.ToString("dd/MM/yyyy")
                        : "—");
                    MetaRow("Ref#", so.RefQuotation ?? "—");
                    MetaRow("Salesperson", so.Sales.Name);
                    MetaRow("Project", so.ProjectName);
                });
            });

            // ── Tabel Items ──
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(22);   // #
                    cols.RelativeColumn(5);    // Item & Deskripsi
                    cols.ConstantColumn(36);   // Qty
                    cols.ConstantColumn(52);   // Qty Shipped
                    cols.ConstantColumn(58);   // Qty Not Shipped
                    cols.ConstantColumn(36);   // UoM
                    cols.ConstantColumn(72);   // Rate
                    cols.ConstantColumn(72);   // Amount
                });

                table.Header(header =>
                {
                    static IContainer HC(IContainer c) => c
                        .Background(Navy).Padding(4).AlignMiddle();

                    void HT(IContainer c, string text, bool right = false)
                    {
                        var t = c.Text(text).FontSize(8).Bold().FontColor(Colors.White);
                        if (right) t.AlignRight();
                    }

                    header.Cell().Element(HC).Element(c => HT(c, "#"));
                    header.Cell().Element(HC).Element(c => HT(c, "Item & Deskripsi"));
                    header.Cell().Element(HC).Element(c => HT(c, "Qty", true));
                    header.Cell().Element(HC).Element(c => HT(c, "Qty Shipped", true));
                    header.Cell().Element(HC).Element(c => HT(c, "Qty Not Shipped", true));
                    header.Cell().Element(HC).Element(c => HT(c, "UoM"));
                    header.Cell().Element(HC).Element(c => HT(c, "Rate", true));
                    header.Cell().Element(HC).Element(c => HT(c, "Amount", true));
                });

                var items = so.Items.OrderBy(x => x.SortOrder).ToList();
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
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
                        if (!string.IsNullOrEmpty(item.Notes))
                            dc.Item().Text(item.Notes)
                                .FontSize(7).FontColor(Colors.Grey.Medium).Italic();
                    });

                    table.Cell().Element(DC).AlignRight()
                        .Text(FormatQty(item.Qty)).FontSize(8);
                    table.Cell().Element(DC).AlignRight()
                        .Text(FormatQty(item.QtyShipped)).FontSize(8);
                    table.Cell().Element(DC).AlignRight()
                        .Text(FormatQty(item.QtyNotShipped)).FontSize(8);
                    table.Cell().Element(DC).Text(item.Uom).FontSize(8);
                    table.Cell().Element(DC).AlignRight()
                        .Text(FormatRupiah(item.UnitPrice)).FontSize(8);
                    table.Cell().Element(DC).AlignRight()
                        .Text(FormatRupiah(item.Amount)).FontSize(8);
                }
            });

            // ── Summary Totals ──
            col.Item().AlignRight().Width(280).Column(summary =>
            {
                summary.Spacing(2);

                summary.Item().Text($"Items in Total {so.Items.Count}")
                    .FontSize(8).FontColor(Colors.Grey.Medium);
                summary.Item().Height(4);

                SummaryRow(summary, "Sub Total", FormatRupiah(subTotal));
                SummaryRow(summary, "PPN (11%)", FormatRupiah(taxAmount));
                SummaryRow(summary, $"Total Rp.", FormatRupiah(grandTotal), bold: true, topBorder: true);
            });

            // ── Notes ──
            if (!string.IsNullOrEmpty(so.Notes))
            {
                col.Item().Column(notes =>
                {
                    notes.Item().Text("Catatan:").FontSize(8).Bold().FontColor(Colors.Grey.Medium);
                    notes.Item().PaddingLeft(8).Text(so.Notes)
                        .FontSize(8).FontColor(Colors.Grey.Darken2);
                });
            }
        });
    }

    // ── Footer ────────────────────────────────────────────────────────────────

    private static void RenderFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().BorderTop(0.5f).BorderColor(SlateGray).PaddingTop(4).Row(row =>
            {
                row.RelativeItem()
                    .Text("Dokumen dicetak otomatis oleh sistem Syntera ERP.")
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
            string fgLabel = color ?? (string)Colors.Grey.Darken2;
            string fgValue = color ?? (string)Colors.Grey.Darken3;
            var fontSize = bold ? 10 : 9;

            var lt = r.RelativeItem().Text(label).FontSize(fontSize).FontColor(fgLabel);
            if (bold) lt.Bold();

            var vt = r.ConstantItem(100).AlignRight().Text(value).FontSize(fontSize).FontColor(fgValue);
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
