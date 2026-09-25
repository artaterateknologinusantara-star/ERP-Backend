using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.VendorPortal;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class VendorRabRequestService : IVendorRabRequestService
{
    private readonly AppDbContext _db;

    public VendorRabRequestService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<VendorRabRequestDto> CreateAndSendAsync(
        Guid quotationGroupId, Guid sentByUserId, CreateVendorRabRequestRequest request)
    {
        _ = await _db.QuotationGroups.FirstOrDefaultAsync(g => g.Id == quotationGroupId)
            ?? throw new KeyNotFoundException("Group tidak ditemukan.");

        var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == request.SupplierId)
            ?? throw new KeyNotFoundException("Supplier tidak ditemukan.");

        var alreadyRequested = await _db.VendorRabRequests
            .AnyAsync(r => r.QuotationGroupId == quotationGroupId && r.SupplierId == request.SupplierId);
        if (alreadyRequested)
            throw new InvalidOperationException(
                "Sudah ada permintaan RAB untuk vendor ini di Group yang sama. Tambahkan baris baru ke request yang sudah ada, bukan bikin request baru.");

        var rabRequest = new VendorRabRequest
        {
            QuotationGroupId = quotationGroupId,
            SupplierId = request.SupplierId,
            Name = request.Name,
            DueDate = request.DueDate,
            Status = VendorRabRequestStatus.Sent,
            SentAt = DateTimeOffset.UtcNow,
            SentBy = sentByUserId,
        };
        _db.VendorRabRequests.Add(rabRequest);

        var lines = request.Lines.Select(l => new VendorRabRequestLine
        {
            VendorRabRequestId = rabRequest.Id,
            Name = l.Name,
            Spesifikasi = l.Spesifikasi,
            Volume = l.Volume,
            Unit = l.Unit,
            SortOrder = l.SortOrder,
        }).ToList();
        _db.VendorRabRequestLines.AddRange(lines);
        rabRequest.Lines = lines;

        await _db.SaveChangesAsync();

        return ToDto(rabRequest, supplier.Name, []);
    }

    public async Task<List<VendorRabRequestDto>> ListByGroupAsync(Guid quotationGroupId)
    {
        var requests = await _db.VendorRabRequests
            .AsNoTracking()
            .Include(r => r.Supplier)
            .Include(r => r.Lines)
            .Include(r => r.Submissions)
            .Where(r => r.QuotationGroupId == quotationGroupId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return requests.Select(r => ToDto(r, r.Supplier.Name, r.Submissions)).ToList();
    }

    public async Task<VendorRabRequestDto?> GetByIdAsync(Guid requestId)
    {
        var r = await LoadDetailAsync(requestId);
        return r is null ? null : ToDto(r, r.Supplier.Name, r.Submissions);
    }

    public async Task<List<VendorRabRequestDto>> ListForVendorAsync(Guid supplierId)
    {
        var requests = await _db.VendorRabRequests
            .AsNoTracking()
            .Include(r => r.Supplier)
            .Include(r => r.Lines)
            .Include(r => r.Submissions)
            .Where(r => r.SupplierId == supplierId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return requests.Select(r => ToDto(r, r.Supplier.Name, r.Submissions)).ToList();
    }

    public async Task<VendorRabRequestDto?> GetForVendorAsync(Guid requestId, Guid supplierId)
    {
        var r = await LoadDetailAsync(requestId);
        // Request milik vendor lain dilaporkan sebagai "tidak ditemukan", bukan "forbidden" —
        // supaya keberadaan request vendor lain tidak bisa dikonfirmasi lewat status code.
        if (r is null || r.SupplierId != supplierId) return null;
        return ToDto(r, r.Supplier.Name, r.Submissions);
    }

    private async Task<VendorRabRequest?> LoadDetailAsync(Guid requestId) =>
        await _db.VendorRabRequests
            .AsNoTracking()
            .Include(r => r.Supplier)
            .Include(r => r.Lines)
            .Include(r => r.Submissions)
            .FirstOrDefaultAsync(r => r.Id == requestId);

    private static VendorRabRequestDto ToDto(
        VendorRabRequest r, string supplierName, IEnumerable<VendorRabSubmission> submissions) => new()
    {
        Id = r.Id,
        QuotationGroupId = r.QuotationGroupId,
        SupplierId = r.SupplierId,
        SupplierName = supplierName,
        Name = r.Name,
        Status = r.Status.ToString(),
        SentAt = r.SentAt,
        DueDate = r.DueDate,
        ApprovedWorkItemId = r.ApprovedWorkItemId,
        Lines = r.Lines.OrderBy(l => l.SortOrder).Select(l => new VendorRabRequestLineDto
        {
            Id = l.Id,
            Name = l.Name,
            Spesifikasi = l.Spesifikasi,
            Volume = l.Volume,
            Unit = l.Unit,
            SortOrder = l.SortOrder,
        }).ToList(),
        Submissions = submissions.OrderByDescending(s => s.AttemptNumber).Select(s => new VendorRabSubmissionSummaryDto
        {
            Id = s.Id,
            AttemptNumber = s.AttemptNumber,
            Status = s.Status.ToString(),
            SubmittedAt = s.SubmittedAt,
        }).ToList(),
    };
}
