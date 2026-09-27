using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Reports;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

// Margin yang benar-benar "terealisasi" ke Quotation hanya ada di VendorRabSubmissionLine
// (breakdown ServicePrice/MaterialPrice vs ServiceMarkup/MaterialMarkup) — setelah approve, sisi
// Quotation/QuotationWorkDetail cuma menyimpan harga jual akhir yang sudah digabung (lihat
// QuotationService.ApplyApprovedVendorRabSubmissionAsync), jadi laporan ini WAJIB query dari sisi
// VendorRabSubmission/Line langsung, bukan dari Quotation. Hanya submission Approved yang dihitung
// — itu representasi margin yang benar-benar masuk ke quotation resmi (bukan PendingReview/
// RevisionRequested/Rejected).
public class SubconMarginReportService : ISubconMarginReportService
{
    private readonly AppDbContext _db;

    public SubconMarginReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<SubconMarginByVendorReportDto> GetByVendorAsync(DateOnly? startDate, DateOnly? endDate, Guid? supplierId)
    {
        var (start, end, lines) = await LoadApprovedLinesAsync(startDate, endDate, supplierId);

        var rows = lines
            .GroupBy(l => new { l.SupplierId, l.SupplierName })
            .Select(g => BuildVendorRow(g.Key.SupplierId, g.Key.SupplierName, g))
            .OrderByDescending(r => r.TotalMargin)
            .ToList();

        var totalNilaiJual = rows.Sum(r => r.TotalNilaiJual);
        var totalMargin = rows.Sum(r => r.TotalMargin);

        return new SubconMarginByVendorReportDto
        {
            StartDate = start,
            EndDate = end,
            Rows = rows,
            TotalNilaiJual = totalNilaiJual,
            TotalMargin = totalMargin,
            MarginPercent = totalNilaiJual > 0 ? totalMargin / totalNilaiJual * 100 : 0,
        };
    }

    public async Task<SubconMarginByQuotationReportDto> GetByQuotationAsync(DateOnly? startDate, DateOnly? endDate, Guid? supplierId)
    {
        var (start, end, lines) = await LoadApprovedLinesAsync(startDate, endDate, supplierId);

        var rows = lines
            .GroupBy(l => new { l.QuotationId, l.QuotationNo, l.ProjectName, l.CustomerName, l.QuotationStatus })
            .Select(g => new SubconMarginByQuotationRowDto
            {
                QuotationId = g.Key.QuotationId,
                No = g.Key.QuotationNo,
                ProjectName = g.Key.ProjectName,
                CustomerName = g.Key.CustomerName,
                Status = g.Key.QuotationStatus.ToString(),
                VendorGroups = g
                    .GroupBy(l => new { l.VendorRabSubmissionId, l.QuotationGroupId, l.GroupName, l.SupplierId, l.SupplierName, l.ReviewedAt })
                    .Select(vg => new SubconMarginQuotationVendorGroupDto
                    {
                        QuotationGroupId = vg.Key.QuotationGroupId,
                        GroupName = vg.Key.GroupName,
                        SupplierId = vg.Key.SupplierId,
                        SupplierName = vg.Key.SupplierName,
                        VendorRabSubmissionId = vg.Key.VendorRabSubmissionId,
                        ReviewedAt = vg.Key.ReviewedAt,
                        TotalNilaiJual = vg.Sum(l => l.NilaiJual),
                        TotalMargin = vg.Sum(l => l.Margin),
                    })
                    .OrderBy(vg => vg.GroupName).ThenBy(vg => vg.SupplierName)
                    .ToList(),
                TotalNilaiJual = g.Sum(l => l.NilaiJual),
                TotalMargin = g.Sum(l => l.Margin),
            })
            .OrderByDescending(r => r.TotalMargin)
            .ToList();

        return new SubconMarginByQuotationReportDto
        {
            StartDate = start,
            EndDate = end,
            Rows = rows,
        };
    }

    private static SubconMarginByVendorRowDto BuildVendorRow(Guid supplierId, string supplierName, IEnumerable<LineRow> group)
    {
        var g = group.ToList();
        var totalNilaiJual = g.Sum(l => l.NilaiJual);
        var totalMargin = g.Sum(l => l.Margin);
        return new SubconMarginByVendorRowDto
        {
            SupplierId = supplierId,
            SupplierName = supplierName,
            RequestCount = g.Select(l => l.VendorRabRequestId).Distinct().Count(),
            ApprovedSubmissionCount = g.Select(l => l.VendorRabSubmissionId).Distinct().Count(),
            TotalNilaiJual = totalNilaiJual,
            TotalMargin = totalMargin,
            MarginPercent = totalNilaiJual > 0 ? totalMargin / totalNilaiJual * 100 : 0,
        };
    }

    private async Task<(DateOnly Start, DateOnly End, List<LineRow> Lines)> LoadApprovedLinesAsync(
        DateOnly? startDate, DateOnly? endDate, Guid? supplierId)
    {
        var end = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = startDate ?? new DateOnly(end.Year, end.Month, 1);

        var startOffset = ToStartOfDay(start);
        var endOffset = ToEndOfDay(end);

        var raw = await _db.VendorRabSubmissionLines
            .Where(l => l.VendorRabSubmission.Status == VendorRabSubmissionStatus.Approved
                     && l.VendorRabSubmission.ReviewedAt != null
                     && l.VendorRabSubmission.ReviewedAt >= startOffset
                     && l.VendorRabSubmission.ReviewedAt <= endOffset
                     && (supplierId == null || l.VendorRabSubmission.VendorRabRequest.SupplierId == supplierId))
            .Select(l => new
            {
                l.Volume,
                l.ServicePrice,
                l.MaterialPrice,
                l.ServiceMarkup,
                l.MaterialMarkup,
                VendorRabSubmissionId = l.VendorRabSubmissionId,
                ReviewedAt = l.VendorRabSubmission.ReviewedAt,
                VendorRabRequestId = l.VendorRabSubmission.VendorRabRequestId,
                SupplierId = l.VendorRabSubmission.VendorRabRequest.SupplierId,
                SupplierName = l.VendorRabSubmission.VendorRabRequest.Supplier.Name,
                QuotationGroupId = l.VendorRabSubmission.VendorRabRequest.QuotationGroupId,
                GroupName = l.VendorRabSubmission.VendorRabRequest.QuotationGroup.Name,
                QuotationId = l.VendorRabSubmission.VendorRabRequest.QuotationGroup.Tab.QuotationId,
                QuotationNo = l.VendorRabSubmission.VendorRabRequest.QuotationGroup.Tab.Quotation.No,
                ProjectName = l.VendorRabSubmission.VendorRabRequest.QuotationGroup.Tab.Quotation.ProjectName,
                CustomerName = l.VendorRabSubmission.VendorRabRequest.QuotationGroup.Tab.Quotation.Customer.Name,
                QuotationStatus = l.VendorRabSubmission.VendorRabRequest.QuotationGroup.Tab.Quotation.Status,
            })
            .ToListAsync();

        var lines = raw.Select(l => new LineRow
        {
            VendorRabSubmissionId = l.VendorRabSubmissionId,
            ReviewedAt = l.ReviewedAt,
            VendorRabRequestId = l.VendorRabRequestId,
            SupplierId = l.SupplierId,
            SupplierName = l.SupplierName,
            QuotationGroupId = l.QuotationGroupId,
            GroupName = l.GroupName,
            QuotationId = l.QuotationId,
            QuotationNo = l.QuotationNo,
            ProjectName = l.ProjectName,
            CustomerName = l.CustomerName,
            QuotationStatus = l.QuotationStatus,
            NilaiJual = l.Volume * (l.ServicePrice + l.ServiceMarkup + l.MaterialPrice + l.MaterialMarkup),
            Margin = l.Volume * (l.ServiceMarkup + l.MaterialMarkup),
        }).ToList();

        return (start, end, lines);
    }

    private static DateTimeOffset ToStartOfDay(DateOnly date) =>
        new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    private static DateTimeOffset ToEndOfDay(DateOnly date) =>
        new(date.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

    private class LineRow
    {
        public Guid VendorRabSubmissionId { get; set; }
        public DateTimeOffset? ReviewedAt { get; set; }
        public Guid VendorRabRequestId { get; set; }
        public Guid SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public Guid QuotationGroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public Guid QuotationId { get; set; }
        public string QuotationNo { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public QuotationStatus QuotationStatus { get; set; }
        public decimal NilaiJual { get; set; }
        public decimal Margin { get; set; }
    }
}
