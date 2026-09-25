using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Quotation;
using SynteraERP.Api.DTOs.VendorPortal;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class VendorRabSubmissionService : IVendorRabSubmissionService
{
    private readonly AppDbContext _db;
    private readonly IQuotationService _quotationSvc;

    public VendorRabSubmissionService(AppDbContext db, IQuotationService quotationSvc)
    {
        _db = db;
        _quotationSvc = quotationSvc;
    }

    public async Task<VendorRabSubmissionDto> CreateAsync(
        Guid requestId, Guid supplierId, Guid portalUserId, CreateVendorRabSubmissionRequest request)
    {
        var rabRequest = await _db.VendorRabRequests
            .Include(r => r.Lines)
            .Include(r => r.Submissions)
            .FirstOrDefaultAsync(r => r.Id == requestId)
            ?? throw new KeyNotFoundException("Permintaan RAB tidak ditemukan.");

        // Dilaporkan sebagai "tidak ditemukan" (bukan forbidden) kalau request bukan milik
        // vendor ini — supaya keberadaan request vendor lain tidak bisa dikonfirmasi.
        if (rabRequest.SupplierId != supplierId)
            throw new KeyNotFoundException("Permintaan RAB tidak ditemukan.");

        if (rabRequest.Status != VendorRabRequestStatus.Sent)
            throw new InvalidOperationException("Permintaan RAB ini sudah tidak menerima submission baru.");

        var hasOpenSubmission = rabRequest.Submissions.Any(s =>
            s.Status is VendorRabSubmissionStatus.PendingReview or VendorRabSubmissionStatus.Approved);
        if (hasOpenSubmission)
            throw new InvalidOperationException(
                "Masih ada submission yang menunggu review atau sudah disetujui untuk permintaan ini.");

        // Reject-all: baris yang dikirim harus PERSIS sama dengan seluruh baris yang diminta —
        // tidak boleh ada yang hilang, tidak dikenal, atau duplikat (prinsip sama seperti
        // validasi import Excel di bagian 5 dokumen rencana, supaya form web dan Excel konsisten).
        var requestLineIds = rabRequest.Lines.Select(l => l.Id).ToHashSet();
        var submittedLineIds = request.Lines.Select(l => l.VendorRabRequestLineId).ToList();

        var unknown = submittedLineIds.Where(id => !requestLineIds.Contains(id)).ToList();
        if (unknown.Count > 0)
            throw new InvalidOperationException(
                $"Ada {unknown.Count} baris yang tidak dikenal (bukan bagian dari permintaan RAB ini).");

        var duplicates = submittedLineIds.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0)
            throw new InvalidOperationException("Ada baris yang dikirim lebih dari sekali dalam 1 submission.");

        var missing = requestLineIds.Except(submittedLineIds).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Ada {missing.Count} baris yang belum diisi harganya. Semua baris permintaan RAB harus diisi.");

        // Sama seperti jalur Excel (VendorRabExcelService) — harga satuan tidak boleh negatif.
        var negativeCount = request.Lines.Count(l => l.ServicePrice < 0 || l.MaterialPrice < 0);
        if (negativeCount > 0)
            throw new InvalidOperationException(
                $"Ada {negativeCount} baris dengan Harga Jasa/Material negatif. Harga tidak boleh negatif.");

        var nextAttempt = (rabRequest.Submissions.Count == 0 ? 0 : rabRequest.Submissions.Max(s => s.AttemptNumber)) + 1;

        var submission = new VendorRabSubmission
        {
            VendorRabRequestId = requestId,
            AttemptNumber = nextAttempt,
            Status = VendorRabSubmissionStatus.PendingReview,
            SubmittedAt = DateTimeOffset.UtcNow,
            SubmittedByPortalUserId = portalUserId,
        };
        _db.VendorRabSubmissions.Add(submission);

        var lines = request.Lines.Select(l => new VendorRabSubmissionLine
        {
            VendorRabSubmissionId = submission.Id,
            VendorRabRequestLineId = l.VendorRabRequestLineId,
            ServicePrice = l.ServicePrice,
            MaterialPrice = l.MaterialPrice,
            ServiceMarkup = 0,
            MaterialMarkup = 0,
        }).ToList();
        _db.VendorRabSubmissionLines.AddRange(lines);
        submission.Lines = lines;

        await _db.SaveChangesAsync();

        return await GetByIdAsync(submission.Id)
            ?? throw new InvalidOperationException("Submission gagal dimuat ulang setelah disimpan.");
    }

    public async Task<VendorRabSubmissionDto?> GetByIdAsync(Guid submissionId)
    {
        var s = await LoadDetailAsync(submissionId);
        return s is null ? null : ToDto(s);
    }

    public async Task<bool> SetLineMarkupAsync(Guid submissionId, Guid lineId, decimal serviceMarkup, decimal materialMarkup)
    {
        var line = await _db.VendorRabSubmissionLines
            .Include(l => l.VendorRabSubmission)
            .FirstOrDefaultAsync(l => l.Id == lineId && l.VendorRabSubmissionId == submissionId);
        if (line is null) return false;

        if (line.VendorRabSubmission.Status != VendorRabSubmissionStatus.PendingReview)
            throw new InvalidOperationException("Markup hanya bisa diubah selama submission masih menunggu review.");

        if (line.ServicePrice + serviceMarkup < 0 || line.MaterialPrice + materialMarkup < 0)
            throw new InvalidOperationException(
                "Harga + Markup tidak boleh negatif (Jasa maupun Material).");

        line.ServiceMarkup = serviceMarkup;
        line.MaterialMarkup = materialMarkup;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<Guid> ApproveAsync(Guid submissionId, Guid approvedByUserId)
    {
        var submission = await _db.VendorRabSubmissions
            .Include(s => s.Lines).ThenInclude(l => l.VendorRabRequestLine)
            .Include(s => s.VendorRabRequest)
            .FirstOrDefaultAsync(s => s.Id == submissionId)
            ?? throw new KeyNotFoundException("Submission tidak ditemukan.");

        if (submission.Status != VendorRabSubmissionStatus.PendingReview)
            throw new InvalidOperationException("Submission ini sudah diputuskan sebelumnya (bukan PendingReview).");

        var applyRequest = new ApplyVendorRabSubmissionRequest
        {
            QuotationGroupId = submission.VendorRabRequest.QuotationGroupId,
            WorkItemName = submission.VendorRabRequest.Name,
            Lines = submission.Lines.Select(l => new ApplyVendorRabSubmissionLine
            {
                Name = l.VendorRabRequestLine.Name,
                Spesifikasi = l.VendorRabRequestLine.Spesifikasi,
                Volume = l.VendorRabRequestLine.Volume,
                Unit = l.VendorRabRequestLine.Unit,
                FinalServicePrice = l.ServicePrice + l.ServiceMarkup,
                FinalMaterialPrice = l.MaterialPrice + l.MaterialMarkup,
                SortOrder = l.VendorRabRequestLine.SortOrder,
            }).ToList(),
        };

        // Satu transaction untuk seluruh "approve": WorkItem/WorkDetail baru (ditulis lewat
        // QuotationService, yang ikut ambient transaction ini alih-alih buka transaction sendiri
        // — lihat komentar di ApplyApprovedVendorRabSubmissionAsync) + status Submission/Request
        // — rule #5, supaya tidak ada state "WorkItem sudah dibuat tapi submission masih
        // PendingReview" kalau salah satu langkah gagal.
        await using var tx = await _db.Database.BeginTransactionAsync();

        var workItemDto = await _quotationSvc.ApplyApprovedVendorRabSubmissionAsync(applyRequest);

        submission.Status = VendorRabSubmissionStatus.Approved;
        submission.ReviewedBy = approvedByUserId;
        submission.ReviewedAt = DateTimeOffset.UtcNow;

        submission.VendorRabRequest.Status = VendorRabRequestStatus.Approved;
        submission.VendorRabRequest.ApprovedWorkItemId = workItemDto.Id;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return workItemDto.Id;
    }

    public async Task<bool> RejectAsync(Guid submissionId, Guid rejectedByUserId, string? reason)
    {
        var submission = await _db.VendorRabSubmissions.FirstOrDefaultAsync(s => s.Id == submissionId);
        if (submission is null) return false;

        if (submission.Status != VendorRabSubmissionStatus.PendingReview)
            throw new InvalidOperationException("Submission ini sudah diputuskan sebelumnya (bukan PendingReview).");

        submission.Status = VendorRabSubmissionStatus.Rejected;
        submission.ReviewedBy = rejectedByUserId;
        submission.ReviewedAt = DateTimeOffset.UtcNow;
        submission.RejectionReason = reason;

        // Request TIDAK berubah status (tetap Sent) — vendor boleh submit ulang sebagai attempt
        // baru (versioning, keputusan produk 24 Sep 2026).
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<VendorRabSubmission?> LoadDetailAsync(Guid submissionId) =>
        await _db.VendorRabSubmissions
            .AsNoTracking()
            .Include(s => s.Lines).ThenInclude(l => l.VendorRabRequestLine)
            .FirstOrDefaultAsync(s => s.Id == submissionId);

    private static VendorRabSubmissionDto ToDto(VendorRabSubmission s) => new()
    {
        Id = s.Id,
        VendorRabRequestId = s.VendorRabRequestId,
        AttemptNumber = s.AttemptNumber,
        Status = s.Status.ToString(),
        SubmittedAt = s.SubmittedAt,
        ReviewedBy = s.ReviewedBy,
        ReviewedAt = s.ReviewedAt,
        RejectionReason = s.RejectionReason,
        Lines = s.Lines.OrderBy(l => l.VendorRabRequestLine.SortOrder).Select(l => new VendorRabSubmissionLineDto
        {
            Id = l.Id,
            VendorRabRequestLineId = l.VendorRabRequestLineId,
            Name = l.VendorRabRequestLine.Name,
            Spesifikasi = l.VendorRabRequestLine.Spesifikasi,
            Volume = l.VendorRabRequestLine.Volume,
            Unit = l.VendorRabRequestLine.Unit,
            ServicePrice = l.ServicePrice,
            MaterialPrice = l.MaterialPrice,
            ServiceMarkup = l.ServiceMarkup,
            MaterialMarkup = l.MaterialMarkup,
            FinalServicePrice = l.ServicePrice + l.ServiceMarkup,
            FinalMaterialPrice = l.MaterialPrice + l.MaterialMarkup,
            TotalHarga = l.VendorRabRequestLine.Volume
                * (l.ServicePrice + l.ServiceMarkup + l.MaterialPrice + l.MaterialMarkup),
        }).ToList(),
    };
}
