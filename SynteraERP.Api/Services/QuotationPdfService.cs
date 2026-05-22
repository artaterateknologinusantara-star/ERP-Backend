using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SynteraERP.Api.Data;
using SynteraERP.Api.Models;

namespace SynteraERP.Api.Services;

public class QuotationPdfService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public QuotationPdfService(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<byte[]?> GenerateAsync(Guid quotationId)
    {
        var quotation = await _db.Quotations
            .Include(q => q.Customer)
            .Include(q => q.Sales)
            .Include(q => q.Tabs)
                .ThenInclude(t => t.Groups)
                    .ThenInclude(g => g.Items)
            .FirstOrDefaultAsync(q => q.Id == quotationId);

        if (quotation is null) return null;

        var company = await _db.CompanySettings.FirstOrDefaultAsync()
            ?? new CompanySettings { CompanyName = "PT Syntera Teknologi Nusantara" };

        // Resolve logo bytes once — null if path missing or file not found
        byte[]? logoBytes = null;
        if (!string.IsNullOrWhiteSpace(company.LogoPath))
        {
            var logoFullPath = Path.Combine(_env.ContentRootPath, "uploads", company.LogoPath);
            if (File.Exists(logoFullPath))
                logoBytes = await File.ReadAllBytesAsync(logoFullPath);
        }

        QuestPDF.Settings.License = LicenseType.Community;

        var lineItems = quotation.Tabs
            .OrderBy(t => t.SortOrder)
            .SelectMany(t => t.Groups.OrderBy(g => g.SortOrder)
                .SelectMany(g => g.Items.OrderBy(i => i.SortOrder)
                    .Select(i => new PdfLineItem(t.Label, g.Name, i))))
            .ToList();

        var pdf = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30, Unit.Point);
                page.DefaultTextStyle(ts => ts.FontSize(9).FontFamily("Arial"));

                page.Header().Element(c => RenderHeader(c, company, quotation, logoBytes));
                page.Content().Element(c => RenderContent(c, company, quotation, lineItems));
                page.Footer().Element(RenderFooter);
            });
        });

        return pdf.GeneratePdf();
    }

    // ─── Header ───────────────────────────────────────────────────────────────

    private static void RenderHeader(IContainer c, CompanySettings company, Quotation q, byte[]? logoBytes)
    {
        c.Column(col =>
        {
            col.Item().Row(row =>
            {
                // Left: Logo + company info
                row.RelativeItem().Row(logoRow =>
                {
                    if (logoBytes is not null)
                    {
                        logoRow.ConstantItem(56).PaddingRight(8).AlignMiddle()
                            .Image(logoBytes).FitWidth();
                    }

                    logoRow.RelativeItem().Column(info =>
                    {
                        info.Item().Text(company.CompanyName)
                            .Bold().FontSize(12).FontColor(Colors.Blue.Darken3);

                        if (!string.IsNullOrWhiteSpace(company.Address))
                            info.Item().Text(company.Address).FontSize(8).FontColor(Colors.Grey.Darken1);

                        if (!string.IsNullOrWhiteSpace(company.Phone))
                            info.Item().Text($"Tel: {company.Phone}").FontSize(8).FontColor(Colors.Grey.Darken1);

                        if (!string.IsNullOrWhiteSpace(company.Email))
                            info.Item().Text($"Email: {company.Email}").FontSize(8).FontColor(Colors.Grey.Darken1);

                        if (!string.IsNullOrWhiteSpace(company.Website))
                            info.Item().Text(company.Website).FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                });

                // Right: Document title block
                row.ConstantItem(155).AlignRight().Column(right =>
                {
                    right.Item().Text("PENAWARAN HARGA")
                        .Bold().FontSize(14).FontColor(Colors.Blue.Darken3);
                    right.Item().Text(q.No).Bold().FontSize(11);
                    if (q.Revision > 0)
                        right.Item().Text($"Revisi ke-{q.Revision}").FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });

            col.Item().PaddingTop(5).LineHorizontal(1.5f).LineColor(Colors.Blue.Darken3);
        });
    }

    // ─── Content ──────────────────────────────────────────────────────────────

    private static void RenderContent(IContainer c, CompanySettings company, Quotation q, List<PdfLineItem> items)
    {
        c.Column(col =>
        {
            col.Spacing(8);

            // Customer + quotation info block
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text("KEPADA:").Bold().FontSize(8).FontColor(Colors.Grey.Darken2);
                    left.Item().Text(q.Customer.Name).Bold();

                    if (!string.IsNullOrWhiteSpace(q.Customer.Address))
                        left.Item().Text(q.Customer.Address).FontSize(8);

                    if (!string.IsNullOrWhiteSpace(q.Customer.ContactPerson))
                        left.Item().Text($"Attn: {q.Customer.ContactPerson}").FontSize(8);

                    left.Item().PaddingTop(4).Text("PROYEK:").Bold().FontSize(8).FontColor(Colors.Grey.Darken2);
                    left.Item().Text(q.ProjectName).Bold();
                });

                row.ConstantItem(180).Table(t =>
                {
                    t.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(3);
                    });

                    void AddRow(string label, string value)
                    {
                        t.Cell().PaddingVertical(2).PaddingHorizontal(4)
                            .Text(label).FontSize(8).FontColor(Colors.Grey.Darken2);
                        t.Cell().PaddingVertical(2).PaddingHorizontal(4)
                            .Text(value).FontSize(8).Bold();
                    }

                    AddRow("No. Penawaran", q.No);
                    AddRow("Tanggal", q.Date.ToString("dd MMMM yyyy"));
                    AddRow("Berlaku s/d", q.ValidUntil.ToString("dd MMMM yyyy"));
                    AddRow("Sales", q.Sales.Name);

                    if (!string.IsNullOrWhiteSpace(q.PaymentTerms))
                        AddRow("Term Pembayaran", q.PaymentTerms);
                });
            });

            // Items table
            // Columns: No | Deskripsi | Qty+Unit | Jasa/Satuan | Material/Satuan | Total
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(22);   // No
                    cols.RelativeColumn(5);    // Deskripsi
                    cols.ConstantColumn(48);   // Qty + Unit
                    cols.ConstantColumn(76);   // Jasa/Satuan
                    cols.ConstantColumn(82);   // Material/Satuan
                    cols.ConstantColumn(76);   // Total
                });

                // Header
                table.Header(h =>
                {
                    void HeaderCell(IContainer cell, string text, bool alignRight = false)
                    {
                        var t = cell.Background(Colors.Blue.Darken3).Padding(4)
                            .Text(text).Bold().FontColor(Colors.White).FontSize(8);
                        if (alignRight) t.AlignRight();
                        else t.AlignCenter();
                    }

                    HeaderCell(h.Cell(), "No");
                    h.Cell().Background(Colors.Blue.Darken3).Padding(4)
                        .Text("Deskripsi").Bold().FontColor(Colors.White).FontSize(8);
                    HeaderCell(h.Cell(), "Qty");
                    HeaderCell(h.Cell(), "Jasa / Satuan", alignRight: true);
                    HeaderCell(h.Cell(), "Material / Satuan", alignRight: true);
                    HeaderCell(h.Cell(), "Total", alignRight: true);
                });

                string? lastGroup = null;
                int rowNum = 1;

                foreach (var entry in items)
                {
                    string groupKey = $"{entry.Tab}|{entry.GroupName}";

                    if (groupKey != lastGroup)
                    {
                        lastGroup = groupKey;
                        table.Cell().ColumnSpan(6)
                            .Background(Colors.Blue.Lighten4)
                            .Padding(4)
                            .Text(entry.GroupName).Bold().FontSize(8);
                    }

                    var item = entry.Item;
                    decimal total = item.Qty * (item.ServicePrice + item.MaterialPrice);
                    string qtyText = item.Qty % 1 == 0
                        ? ((int)item.Qty).ToString()
                        : item.Qty.ToString("N2");

                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .Text(rowNum.ToString()).AlignCenter();

                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .Column(desc =>
                        {
                            desc.Item().Text(item.Equipment).Bold();

                            if (!string.IsNullOrWhiteSpace(item.Description))
                                desc.Item().Text(item.Description).FontSize(7.5f).FontColor(Colors.Grey.Darken1);

                            if (!string.IsNullOrWhiteSpace(item.Manufacturer))
                                desc.Item().Text($"Merk: {item.Manufacturer}").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                        });

                    // Qty + Unit (unit shown smaller below)
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .Column(qtyCol =>
                        {
                            qtyCol.Item().AlignCenter().Text(qtyText).Bold();
                            qtyCol.Item().AlignCenter().Text(item.Unit).FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                        });

                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .Text(FormatRupiah(item.ServicePrice)).AlignRight();

                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .Text(FormatRupiah(item.MaterialPrice)).AlignRight();

                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .Text(FormatRupiah(total)).AlignRight();

                    rowNum++;
                }
            });

            // Summary block
            col.Item().AlignRight().Width(220).Table(t =>
            {
                t.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(2);
                    cols.RelativeColumn(3);
                });

                decimal subtotalBase = q.TotalMaterial + q.TotalService;
                decimal discountAmt = subtotalBase * (q.Discount / 100);

                void SumRow(string label, string value)
                {
                    t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .PaddingVertical(3).PaddingHorizontal(4)
                        .Text(label).FontSize(8).FontColor(Colors.Grey.Darken2);
                    t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .PaddingVertical(3).PaddingHorizontal(4)
                        .Text(value).FontSize(8).AlignRight();
                }

                SumRow("Subtotal", FormatRupiah(subtotalBase));

                if (q.Discount > 0)
                {
                    SumRow($"Diskon ({q.Discount:N0}%)", $"- {FormatRupiah(discountAmt)}");
                    SumRow("Setelah Diskon", FormatRupiah(subtotalBase - discountAmt));
                }

                SumRow($"PPN ({q.TaxRate:N0}%)", FormatRupiah(q.TaxAmount));

                // Grand total — highlighted
                t.Cell().BorderBottom(1.5f).BorderColor(Colors.Blue.Darken3)
                    .PaddingVertical(3).PaddingHorizontal(4)
                    .Text("GRAND TOTAL").Bold().FontSize(8).FontColor(Colors.Blue.Darken3);
                t.Cell().BorderBottom(1.5f).BorderColor(Colors.Blue.Darken3)
                    .PaddingVertical(3).PaddingHorizontal(4)
                    .Text(FormatRupiah(q.GrandTotal)).Bold().FontSize(8).FontColor(Colors.Blue.Darken3).AlignRight();
            });

            // Notes
            if (!string.IsNullOrWhiteSpace(q.Notes) || !string.IsNullOrWhiteSpace(q.AdditionalNotes))
            {
                col.Item().Column(notes =>
                {
                    notes.Item().Text("Catatan:").Bold().FontSize(8);

                    if (!string.IsNullOrWhiteSpace(q.Notes))
                        notes.Item().Text(q.Notes).FontSize(8);

                    if (!string.IsNullOrWhiteSpace(q.AdditionalNotes))
                        notes.Item().Text(q.AdditionalNotes).FontSize(8);
                });
            }

            // Terms + Signature
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(terms =>
                {
                    terms.Item().Text("SYARAT & KETENTUAN").Bold().FontSize(8).FontColor(Colors.Grey.Darken2);
                    terms.Item().PaddingTop(2)
                        .Text(!string.IsNullOrWhiteSpace(company.FooterText)
                            ? company.FooterText
                            : "Penawaran ini berlaku 14 hari sejak tanggal dikeluarkan.")
                        .FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(140).AlignCenter().Column(sig =>
                {
                    sig.Item().AlignCenter().Text("Hormat Kami,").FontSize(8);
                    sig.Item().AlignCenter().Text(company.CompanyName).Bold().FontSize(8);
                    sig.Item().Height(45);
                    sig.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
                    sig.Item().PaddingTop(2).AlignCenter()
                        .Text(!string.IsNullOrWhiteSpace(company.SignatureName)
                            ? company.SignatureName : "_____________")
                        .Bold().FontSize(8);

                    if (!string.IsNullOrWhiteSpace(company.SignatureTitle))
                        sig.Item().AlignCenter()
                            .Text(company.SignatureTitle)
                            .FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                });
            });
        });
    }

    // ─── Footer ───────────────────────────────────────────────────────────────

    private static void RenderFooter(IContainer c)
    {
        c.Row(row =>
        {
            row.RelativeItem().AlignLeft().Text(t =>
            {
                t.Span("Dokumen dicetak otomatis oleh Syntera ERP.")
                    .FontSize(7).FontColor(Colors.Grey.Medium);
            });

            row.RelativeItem().AlignRight().Text(t =>
            {
                t.Span("Halaman ").FontSize(7).FontColor(Colors.Grey.Medium);
                t.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                t.Span(" dari ").FontSize(7).FontColor(Colors.Grey.Medium);
                t.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
            });
        });
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static string FormatRupiah(decimal value) => $"Rp {value:N0}";

    private record PdfLineItem(string Tab, string GroupName, QuotationItem Item);
}
