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

        // Vendor menyusun RAB-nya sendiri dari nol sekarang — bukan lagi isi-harga-ke-baris-
        // yang-diminta, jadi tidak ada lagi pencocokan terhadap rabRequest.Lines di sini.
        // Validasi standar saja, sama untuk jalur form web maupun Excel (ParseSubmission sudah
        // menolak per-baris duluan di jalur Excel; ini jaring pengaman terakhir yang berlaku sama
        // untuk kedua jalur).
        if (request.Lines.Count == 0)
            throw new InvalidOperationException("Submission harus punya minimal 1 baris.");

        var emptyNameCount = request.Lines.Count(l => string.IsNullOrWhiteSpace(l.Name));
        if (emptyNameCount > 0)
            throw new InvalidOperationException($"Ada {emptyNameCount} baris dengan Nama Item kosong.");

        var emptyUnitCount = request.Lines.Count(l => string.IsNullOrWhiteSpace(l.Unit));
        if (emptyUnitCount > 0)
            throw new InvalidOperationException($"Ada {emptyUnitCount} baris dengan Satuan kosong.");

        var badVolumeCount = request.Lines.Count(l => l.Volume <= 0);
        if (badVolumeCount > 0)
            throw new InvalidOperationException($"Ada {badVolumeCount} baris dengan Volume tidak lebih dari 0.");

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

        var lines = request.Lines.Select((l, index) => new VendorRabSubmissionLine
        {
            VendorRabSubmissionId = submission.Id,
            WorkItemName = l.WorkItemName,
            Name = l.Name,
            Spesifikasi = l.Spesifikasi,
            Volume = l.Volume,
            Unit = l.Unit,
            SortOrder = index,
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

    public async Task<List<Guid>> ApproveAsync(Guid submissionId, Guid approvedByUserId)
    {
        var submission = await _db.VendorRabSubmissions
            .Include(s => s.Lines)
            .Include(s => s.VendorRabRequest)
            .FirstOrDefaultAsync(s => s.Id == submissionId)
            ?? throw new KeyNotFoundException("Submission tidak ditemukan.");

        if (submission.Status != VendorRabSubmissionStatus.PendingReview)
            throw new InvalidOperationException("Submission ini sudah diputuskan sebelumnya (bukan PendingReview).");

        var applyRequest = new ApplyVendorRabSubmissionRequest
        {
            QuotationGroupId = submission.VendorRabRequest.QuotationGroupId,
            SourceVendorRabRequestId = submission.VendorRabRequestId,
            DefaultWorkItemName = submission.VendorRabRequest.Name,
            Lines = submission.Lines.OrderBy(l => l.SortOrder).Select(l => new ApplyVendorRabSubmissionLine
            {
                WorkItemName = l.WorkItemName,
                Name = l.Name,
                Spesifikasi = l.Spesifikasi,
                Volume = l.Volume,
                Unit = l.Unit,
                FinalServicePrice = l.ServicePrice + l.ServiceMarkup,
                FinalMaterialPrice = l.MaterialPrice + l.MaterialMarkup,
                SortOrder = l.SortOrder,
            }).ToList(),
        };

        // Satu transaction untuk seluruh "approve": WorkItem/WorkDetail baru (ditulis lewat
        // QuotationService, yang ikut ambient transaction ini alih-alih buka transaction sendiri
        // — lihat komentar di ApplyApprovedVendorRabSubmissionAsync) + status Submission/Request
        // — rule #5, supaya tidak ada state "WorkItem sudah dibuat tapi submission masih
        // PendingReview" kalau salah satu langkah gagal.
        await using var tx = await _db.Database.BeginTransactionAsync();

        var workItemDtos = await _quotationSvc.ApplyApprovedVendorRabSubmissionAsync(applyRequest);

        submission.Status = VendorRabSubmissionStatus.Approved;
        submission.ReviewedBy = approvedByUserId;
        submission.ReviewedAt = DateTimeOffset.UtcNow;

        submission.VendorRabRequest.Status = VendorRabRequestStatus.Approved;
        // Jejak WorkItem hasil approval ada di QuotationWorkItem.SourceVendorRabRequestId sendiri
        // (ditulis oleh ApplyApprovedVendorRabSubmissionAsync) — tidak ada lagi field di
        // VendorRabRequest yang perlu di-set di sini, karena approval 1 submission sekarang bisa
        // fan-out jadi banyak WorkItem, bukan 1.

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return workItemDtos.Select(w => w.Id).ToList();
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

    public async Task<bool> RequestRevisionAsync(
        Guid submissionId, List<(Guid lineId, string note)> flaggedLines, Guid requestedByUserId)
    {
        var submission = await _db.VendorRabSubmissions
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == submissionId);
        if (submission is null) return false;

        if (submission.Status != VendorRabSubmissionStatus.PendingReview)
            throw new InvalidOperationException("Submission ini sudah diputuskan sebelumnya (bukan PendingReview).");

        if (flaggedLines.Count == 0)
            throw new InvalidOperationException("Pilih minimal 1 baris untuk diminta revisi.");

        var lineIds = submission.Lines.Select(l => l.Id).ToHashSet();
        var unknown = flaggedLines.Select(f => f.lineId).Where(id => !lineIds.Contains(id)).ToList();
        if (unknown.Count > 0)
            throw new InvalidOperationException(
                $"Ada {unknown.Count} baris yang tidak dikenal (bukan bagian dari submission ini).");

        var emptyNoteCount = flaggedLines.Count(f => string.IsNullOrWhiteSpace(f.note));
        if (emptyNoteCount > 0)
            throw new InvalidOperationException($"Ada {emptyNoteCount} baris yang ditandai tanpa catatan revisi.");

        // Tidak membuat attempt/row baru di sini — cuma menandai baris yang perlu diubah vendor.
        // Baris yang TIDAK di-flag di-clear NegotiationNote-nya (kalau ada sisa dari permintaan
        // revisi sebelumnya) supaya vendor tidak salah kira baris itu masih perlu diubah.
        foreach (var line in submission.Lines)
        {
            var flagged = flaggedLines.FirstOrDefault(f => f.lineId == line.Id);
            line.NegotiationNote = flagged.lineId == line.Id ? flagged.note : null;
        }

        submission.Status = VendorRabSubmissionStatus.RevisionRequested;
        submission.ReviewedBy = requestedByUserId;
        submission.ReviewedAt = DateTimeOffset.UtcNow;

        // Request TIDAK berubah status (tetap Sent) — vendor boleh submit ulang sebagai attempt
        // berikutnya, persis seperti alur Reject (bukan clone-forward attempt baru di sini).
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<VendorRabSubmission?> LoadDetailAsync(Guid submissionId) =>
        await _db.VendorRabSubmissions
            .AsNoTracking()
            .Include(s => s.Lines)
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
        Lines = s.Lines.OrderBy(l => l.SortOrder).Select(l => new VendorRabSubmissionLineDto
        {
            Id = l.Id,
            VendorRabRequestLineId = l.VendorRabRequestLineId,
            WorkItemName = l.WorkItemName,
            Name = l.Name,
            Spesifikasi = l.Spesifikasi,
            Volume = l.Volume,
            Unit = l.Unit,
            SortOrder = l.SortOrder,
            ServicePrice = l.ServicePrice,
            MaterialPrice = l.MaterialPrice,
            ServiceMarkup = l.ServiceMarkup,
            MaterialMarkup = l.MaterialMarkup,
            FinalServicePrice = l.ServicePrice + l.ServiceMarkup,
            FinalMaterialPrice = l.MaterialPrice + l.MaterialMarkup,
            TotalHarga = l.Volume * (l.ServicePrice + l.ServiceMarkup + l.MaterialPrice + l.MaterialMarkup),
            NegotiationNote = l.NegotiationNote,
        }).ToList(),
    };
}
