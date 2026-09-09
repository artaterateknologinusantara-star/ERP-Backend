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
    private readonly ILogger<QuotationPdfService> _logger;

    public QuotationPdfService(AppDbContext db, IWebHostEnvironment env, ILogger<QuotationPdfService> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
    }

    public async Task<byte[]?> GenerateAsync(Guid quotationId)
    {
        var quotation = await _db.Quotations
            .Include(q => q.Customer)
            .Include(q => q.Sales)
            .Include(q => q.Tabs)
                .ThenInclude(t => t.Groups)
                    .ThenInclude(g => g.Items)
            .Include(q => q.Tabs)
                .ThenInclude(t => t.Groups)
                    .ThenInclude(g => g.WorkItems)
                        .ThenInclude(w => w.WorkDetails)
                            .ThenInclude(d => d.Attachments)
            .FirstOrDefaultAsync(q => q.Id == quotationId);

        if (quotation is null) return null;

        var company = await _db.CompanySettings.FirstOrDefaultAsync()
            ?? new CompanySettings { CompanyName = "Perusahaan Anda" };

        // Resolve logo bytes once — null if path missing or file not found
        byte[]? logoBytes = null;
        if (!string.IsNullOrWhiteSpace(company.LogoPath))
        {
            var logoFullPath = Path.Combine(_env.ContentRootPath, "uploads", company.LogoPath);
            if (File.Exists(logoFullPath))
                logoBytes = await File.ReadAllBytesAsync(logoFullPath);
        }

        // Resolve Detail Kerja attachment bytes once (Civil ME only) — same reasoning as the
        // logo above: file I/O happens here, not inside the QuestPDF layout callback.
        var attachmentBytes = new Dictionary<Guid, byte[]>();
        if (quotation.IsCivilMeMode)
        {
            var attachments = quotation.Tabs.SelectMany(t => t.Groups)
                .SelectMany(g => g.WorkItems).SelectMany(w => w.WorkDetails)
                .SelectMany(d => d.Attachments);
            foreach (var attachment in attachments)
            {
                var fullPath = Path.Combine(_env.ContentRootPath, "uploads", attachment.FilePath);
                if (File.Exists(fullPath))
                {
                    var bytes = await File.ReadAllBytesAsync(fullPath);
                    try
                    {
                        // QuestPDF/Skia must be able to decode this or .Image() throws mid-layout,
                        // aborting the ENTIRE PDF export — probe-decode up front so one bad image
                        // can't take down the whole document (same defensive spirit as the old
                        // RAB-PDF-merge code this replaced).
                        _ = QuestPDF.Infrastructure.Image.FromBinaryData(bytes);
                        attachmentBytes[attachment.Id] = bytes;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Gambar Detail Kerja {AttachmentId} ({FileName}) tidak bisa di-decode, dilewati dari PDF.",
                            attachment.Id, attachment.FileName);
                    }
                }
                else
                {
                    _logger.LogWarning("Gambar Detail Kerja {AttachmentId} ({FileName}) tidak ditemukan di disk: {Path}",
                        attachment.Id, attachment.FileName, attachment.FilePath);
                }
            }
        }

        QuestPDF.Settings.License = LicenseType.Community;

        var lineItems = quotation.Tabs
            .OrderBy(t => t.SortOrder)
            .SelectMany(t => t.Groups.OrderBy(g => g.SortOrder)
                .SelectMany(g => g.Items.OrderBy(i => i.SortOrder)
                    .Select(i => new PdfLineItem(t.Label, g.Id, g.Name, i))))
            .ToList();

        var pdf = Document.Create(doc =>
        {
            if (quotation.IsCivilMeMode)
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30, Unit.Point);
                    page.DefaultTextStyle(ts => ts.FontSize(9).FontFamily("Arial"));

                    page.Header().Element(c => RenderHeader(c, company, quotation, logoBytes));
                    page.Content().Element(c => RenderRecapContent(c, quotation));
                    page.Footer().Element(c => RenderFooter(c, company.CompanyName));
                });

                if (quotation.Tabs.SelectMany(t => t.Groups).Any(g => g.WorkItems.Count > 0))
                {
                    doc.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(30, Unit.Point);
                        page.DefaultTextStyle(ts => ts.FontSize(9).FontFamily("Arial"));

                        page.Header().Element(c => RenderHeader(c, company, quotation, logoBytes));
                        page.Content().Element(c => RenderWorkItemsContent(c, quotation, attachmentBytes));
                        page.Footer().Element(c => RenderFooter(c, company.CompanyName));
                    });
                }
            }

            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30, Unit.Point);
                page.DefaultTextStyle(ts => ts.FontSize(9).FontFamily("Arial"));

                page.Header().Element(c => RenderHeader(c, company, quotation, logoBytes));
                page.Content().Element(c => RenderContent(c, company, quotation, lineItems));
                page.Footer().Element(c => RenderFooter(c, company.CompanyName));
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

    // ─── Recapitulation (Civil & ME mode only) ───────────────────────────────────

    private static void RenderRecapContent(IContainer c, Quotation q)
    {
        c.Column(col =>
        {
            col.Spacing(8);

            col.Item().Text("RECAPITULATION").Bold().FontSize(12).FontColor(Colors.Blue.Darken3);

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(22);   // No
                    cols.RelativeColumn(5);    // Deskripsi
                    cols.ConstantColumn(60);   // Volume
                    cols.ConstantColumn(50);   // Satuan
                    cols.ConstantColumn(90);   // Harga/Satuan
                    cols.ConstantColumn(90);   // Total
                });

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
                    HeaderCell(h.Cell(), "Volume");
                    HeaderCell(h.Cell(), "Satuan");
                    HeaderCell(h.Cell(), "Harga / Satuan", alignRight: true);
                    HeaderCell(h.Cell(), "Total", alignRight: true);
                });

                var groups = q.Tabs.OrderBy(t => t.SortOrder)
                    .SelectMany(t => t.Groups.OrderBy(g => g.SortOrder))
                    .ToList();

                int no = 1;
                decimal grandTotal = 0;

                foreach (var g in groups)
                {
                    decimal groupTotal = g.FinalSellingPrice ?? 0;
                    decimal volume = g.RecapVolume ?? 1;
                    string unit = string.IsNullOrWhiteSpace(g.RecapUnit) ? "Ls" : g.RecapUnit;
                    decimal pricePerUnit = volume != 0 ? groupTotal / volume : 0;
                    grandTotal += groupTotal;

                    string volumeText = volume % 1 == 0 ? ((int)volume).ToString() : volume.ToString("N2");

                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .Text(no.ToString()).AlignCenter();
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .Text(g.Name).Bold();
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .Text(volumeText).AlignCenter();
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .Text(unit).AlignCenter();
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .Text(FormatRupiah(pricePerUnit)).AlignRight();
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .Text(FormatRupiah(groupTotal)).AlignRight();

                    no++;
                }

                table.Cell().ColumnSpan(5).Background(Colors.Grey.Lighten3).Padding(4)
                    .Text("GRAND TOTAL").Bold().FontSize(9).AlignRight();
                table.Cell().Background(Colors.Grey.Lighten3).Padding(4)
                    .Text(FormatRupiah(grandTotal)).Bold().FontSize(9).AlignRight();

                if (q.TotalAreaSqm is > 0)
                {
                    decimal pricePerSqm = grandTotal / q.TotalAreaSqm.Value;
                    table.Cell().ColumnSpan(5).Background(Colors.Blue.Lighten4).Padding(4)
                        .Text("HARGA / M²").Bold().FontSize(9).AlignRight();
                    table.Cell().Background(Colors.Blue.Lighten4).Padding(4)
                        .Text(FormatRupiah(pricePerSqm)).Bold().FontSize(9).AlignRight();
                }
            });
        });
    }

    // ─── Bill of Quantity — Item Pekerjaan / Detail Kerja (Civil & ME mode only) ─

    private static void RenderWorkItemsContent(IContainer c, Quotation q, Dictionary<Guid, byte[]> attachmentBytes)
    {
        c.Column(col =>
        {
            col.Spacing(10);

            col.Item().Text("BILL OF QUANTITY").Bold().FontSize(12).FontColor(Colors.Blue.Darken3);

            var groups = q.Tabs.OrderBy(t => t.SortOrder)
                .SelectMany(t => t.Groups.OrderBy(g => g.SortOrder))
                .Where(g => g.WorkItems.Count > 0)
                .ToList();

            foreach (var group in groups)
            {
                col.Item().PaddingTop(6).Text(group.Name).Bold().FontSize(10).FontColor(Colors.Blue.Darken2);

                foreach (var workItem in group.WorkItems.OrderBy(w => w.SortOrder))
                {
                    col.Item().PaddingTop(3).Text(workItem.Name).Bold().FontSize(9);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(20);   // No
                            cols.RelativeColumn(3);    // Detail Kerja
                            cols.RelativeColumn(4);    // Spesifikasi
                            cols.ConstantColumn(45);   // Vol
                            cols.ConstantColumn(40);   // Sat
                            cols.ConstantColumn(75);   // Harga Satuan
                            cols.ConstantColumn(75);   // Total
                        });

                        table.Header(h =>
                        {
                            void HeaderCell(IContainer cell, string text, bool alignRight = false)
                            {
                                var t = cell.Background(Colors.Grey.Darken1).Padding(3)
                                    .Text(text).Bold().FontColor(Colors.White).FontSize(7);
                                if (alignRight) t.AlignRight();
                                else t.AlignCenter();
                            }

                            HeaderCell(h.Cell(), "No");
                            h.Cell().Background(Colors.Grey.Darken1).Padding(3)
                                .Text("Detail Kerja").Bold().FontColor(Colors.White).FontSize(7);
                            h.Cell().Background(Colors.Grey.Darken1).Padding(3)
                                .Text("Spesifikasi").Bold().FontColor(Colors.White).FontSize(7);
                            HeaderCell(h.Cell(), "Vol");
                            HeaderCell(h.Cell(), "Sat");
                            HeaderCell(h.Cell(), "Harga Satuan", alignRight: true);
                            HeaderCell(h.Cell(), "Total", alignRight: true);
                        });

                        int no = 1;
                        foreach (var detail in workItem.WorkDetails.OrderBy(d => d.SortOrder))
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                .Text(no.ToString()).FontSize(7).AlignCenter();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                .Text(detail.Name).FontSize(7);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                .Text(detail.Spesifikasi ?? "-").FontSize(7);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                .Text(detail.Volume.ToString("N2")).FontSize(7).AlignCenter();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                .Text(detail.Unit).FontSize(7).AlignCenter();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                .Text(FormatRupiah(detail.UnitPrice)).FontSize(7).AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                .Text(FormatRupiah(detail.TotalHarga)).FontSize(7).AlignRight();

                            no++;
                        }
                    });

                    // Gambar pendukung per Detail Kerja, ditempel di bawah tabel.
                    foreach (var detail in workItem.WorkDetails.OrderBy(d => d.SortOrder))
                    {
                        var images = detail.Attachments.OrderBy(a => a.SortOrder)
                            .Select(a => attachmentBytes.TryGetValue(a.Id, out var bytes) ? bytes : null)
                            .Where(b => b is not null)
                            .Select(b => b!)
                            .ToList();
                        if (images.Count == 0) continue;

                        col.Item().PaddingTop(2).Text($"Gambar — {detail.Name}")
                            .FontSize(7).Italic().FontColor(Colors.Grey.Darken1);
                        col.Item().Row(row =>
                        {
                            foreach (var bytes in images)
                                row.RelativeItem().Padding(2).Image(bytes).FitWidth();
                        });
                    }
                }
            }
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
                });
            });

            // Items table (Quotation Standard only — Civil & ME is priced per Group via
            // Subkontraktor SOW and already covered by the Recapitulation/BOQ pages above,
            // so an equipment/material table here would just render empty).
            // Columns: No | Deskripsi | Qty+Unit | Jasa/Satuan | Material/Satuan | Total
            if (!q.IsCivilMeMode)
            {
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

                Guid? lastGroupId = null;
                string lastGroupName = "";
                decimal groupSubtotal = 0;
                int rowNum = 1;

                void RenderGroupSubtotal()
                {
                    if (lastGroupId == null) return;
                    table.Cell().ColumnSpan(5)
                        .Background(Colors.Grey.Lighten3)
                        .Padding(4)
                        .Text($"Subtotal - {lastGroupName}").Bold().FontSize(8).AlignRight();
                    table.Cell()
                        .Background(Colors.Grey.Lighten3)
                        .Padding(4)
                        .Text(FormatRupiah(groupSubtotal)).Bold().FontSize(8).AlignRight();
                }

                foreach (var entry in items)
                {
                    if (entry.GroupId != lastGroupId)
                    {
                        RenderGroupSubtotal();
                        lastGroupId = entry.GroupId;
                        lastGroupName = entry.GroupName;
                        groupSubtotal = 0;
                        table.Cell().ColumnSpan(6)
                            .Background(Colors.Blue.Lighten4)
                            .Padding(4)
                            .Text(entry.GroupName).Bold().FontSize(8);
                    }

                    var item = entry.Item;
                    decimal total = item.Qty * (item.ServicePrice + item.MaterialPrice);
                    groupSubtotal += total;
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

                RenderGroupSubtotal();
            });
            }

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
                        .Text(!string.IsNullOrWhiteSpace(q.TermsAndConditions)
                            ? q.TermsAndConditions
                            : !string.IsNullOrWhiteSpace(company.FooterText)
                                ? company.FooterText
                                : "Penawaran ini berlaku 14 hari sejak tanggal dikeluarkan.")
                        .FontSize(7.5f).FontColor(Colors.Grey.Darken1);

                    if (!string.IsNullOrWhiteSpace(q.PaymentTerms))
                    {
                        terms.Item().PaddingTop(6).Text("TERM PEMBAYARAN").Bold().FontSize(8).FontColor(Colors.Grey.Darken2);
                        terms.Item().PaddingTop(2).Text(q.PaymentTerms).FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                    }
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

    private static void RenderFooter(IContainer c, string? companyName)
    {
        var footerText = string.IsNullOrWhiteSpace(companyName)
            ? "Dokumen dicetak otomatis oleh sistem."
            : $"Dokumen dicetak otomatis oleh sistem {companyName}.";

        c.Row(row =>
        {
            row.RelativeItem().AlignLeft().Text(t =>
            {
                t.Span(footerText)
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

    private record PdfLineItem(string Tab, Guid GroupId, string GroupName, QuotationItem Item);
}
