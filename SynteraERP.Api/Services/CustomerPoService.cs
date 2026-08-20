using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.CustomerPO;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class CustomerPoService : ICustomerPoService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public CustomerPoService(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<PaginatedResponse<CustomerPoListDto>> ListAsync(PaginationParams p)
    {
        var q = _db.CustomerPOs
            .Include(c => c.Quotation).ThenInclude(q => q.Customer)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(p.Search))
        {
            var s = p.Search.ToLower();
            q = q.Where(c => c.PoNo.ToLower().Contains(s)
                || c.Quotation.No.ToLower().Contains(s)
                || c.Quotation.Customer.Name.ToLower().Contains(s)
                || c.Quotation.ProjectName.ToLower().Contains(s));
        }

        q = p.SortBy switch
        {
            "poNo" => p.IsDescending ? q.OrderByDescending(c => c.PoNo) : q.OrderBy(c => c.PoNo),
            "poDate" => p.IsDescending ? q.OrderByDescending(c => c.PoDate) : q.OrderBy(c => c.PoDate),
            "amount" => p.IsDescending ? q.OrderByDescending(c => c.Amount) : q.OrderBy(c => c.Amount),
            _ => q.OrderByDescending(c => c.CreatedAt),
        };

        var total = await q.CountAsync();
        var data = await q.Skip(p.Skip).Take(p.PerPage).ToListAsync();

        // Load associated SalesOrders in one query, keyed by QuotationId
        var quotationIds = data.Select(c => c.QuotationId).Distinct().ToList();
        var soMap = new Dictionary<Guid, (Guid Id, string No)>();
        if (quotationIds.Count > 0)
        {
            var soList = await _db.SalesOrders
                .Where(s => s.QuotationId != null && quotationIds.Contains(s.QuotationId!.Value))
                .Select(s => new { s.Id, s.No, QuotationId = s.QuotationId!.Value })
                .ToListAsync();
            foreach (var s in soList)
                soMap.TryAdd(s.QuotationId, (s.Id, s.No));
        }

        return PaginatedResponse<CustomerPoListDto>.Create(
            data.Select(c =>
            {
                var found = soMap.TryGetValue(c.QuotationId, out var so);
                return ToListDto(c, found ? so.Id : null, found ? so.No : null);
            }).ToList(),
            total, p.Page, p.PerPage);
    }

    public async Task<CustomerPoDto?> GetByIdAsync(Guid id)
    {
        var cpo = await _db.CustomerPOs
            .Include(c => c.Quotation).ThenInclude(q => q.Customer)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cpo is null) return null;
        var so = await _db.SalesOrders
            .FirstOrDefaultAsync(s => s.QuotationId == cpo.QuotationId && !s.IsDeleted);
        return ToDto(cpo, so?.Id, so?.No);
    }

    public async Task<CustomerPoDto?> GetByQuotationIdAsync(Guid quotationId)
    {
        var cpo = await _db.CustomerPOs
            .Include(c => c.Quotation).ThenInclude(q => q.Customer)
            .FirstOrDefaultAsync(c => c.QuotationId == quotationId);

        if (cpo is null) return null;
        var so = await _db.SalesOrders
            .FirstOrDefaultAsync(s => s.QuotationId == quotationId && !s.IsDeleted);
        return ToDto(cpo, so?.Id, so?.No);
    }

    public async Task<CustomerPoDto> CreateAsync(CreateCustomerPoRequest request, IFormFile? attachment)
    {
        var quotation = await _db.Quotations
            .Include(q => q.Customer)
            .FirstOrDefaultAsync(q => q.Id == request.QuotationId)
            ?? throw new KeyNotFoundException("Quotation tidak ditemukan.");

        if (quotation.Status != QuotationStatus.Disetujui)
            throw new InvalidOperationException("Customer PO hanya dapat diinput untuk Quotation yang sudah Disetujui.");

        var existing = await _db.CustomerPOs.AnyAsync(c => c.QuotationId == request.QuotationId);
        if (existing)
            throw new InvalidOperationException("Customer PO untuk Quotation ini sudah diinput.");

        // Amount validation: PO cannot exceed approved quotation total
        if (request.Amount > quotation.GrandTotal)
            throw new InvalidOperationException(
                $"Nilai PO ({request.Amount:N0}) tidak boleh melebihi total penawaran yang disetujui ({quotation.GrandTotal:N0}).");

        // When PO is lower than quotation, a reason (notes) is mandatory for audit trail
        if (request.Amount < quotation.GrandTotal && string.IsNullOrWhiteSpace(request.Notes))
            throw new InvalidOperationException(
                "Nilai PO berbeda dari penawaran. Wajib mengisi catatan alasan perbedaan nilai.");

        string? attachmentPath = null;
        string? attachmentName = null;

        if (attachment is { Length: > 0 })
        {
            var uploadsDir = Path.Combine(_env.ContentRootPath, "uploads", "customer-po");
            Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(attachment.FileName);
            var storedName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploadsDir, storedName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await attachment.CopyToAsync(stream);

            attachmentPath = Path.Combine("customer-po", storedName);
            attachmentName = attachment.FileName;
        }

        var cpo = new CustomerPO
        {
            QuotationId = request.QuotationId,
            PoNo = request.PoNo,
            PoDate = request.PoDate,
            Amount = request.Amount,
            Notes = request.Notes,
            AttachmentPath = attachmentPath,
            AttachmentName = attachmentName,
        };

        _db.CustomerPOs.Add(cpo);

        // Advance quotation to Selesai — it is now execution-ready
        quotation.Status = QuotationStatus.Selesai;
        quotation.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return (await GetByIdAsync(cpo.Id))!;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var cpo = await _db.CustomerPOs.FindAsync(id);
        if (cpo is null) return false;

        // Remove stored file if present
        if (cpo.AttachmentPath is not null)
        {
            var fullPath = Path.Combine(_env.ContentRootPath, "uploads", cpo.AttachmentPath);
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }

        cpo.IsDeleted = true;
        cpo.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<(byte[] data, string contentType, string fileName)?> GetAttachmentAsync(Guid id)
    {
        var cpo = await _db.CustomerPOs.FindAsync(id);
        if (cpo is null || cpo.AttachmentPath is null) return null;

        var fullPath = Path.Combine(_env.ContentRootPath, "uploads", cpo.AttachmentPath);
        if (!File.Exists(fullPath)) return null;

        var data = await File.ReadAllBytesAsync(fullPath);
        var contentType = GetContentType(cpo.AttachmentPath);
        var fileName = cpo.AttachmentName ?? Path.GetFileName(cpo.AttachmentPath);
        return (data, contentType, fileName);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static string GetContentType(string path) =>
        Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream",
        };

    private static CustomerPoListDto ToListDto(CustomerPO c, Guid? soId, string? soNo) => new()
    {
        Id = c.Id,
        PoNo = c.PoNo,
        QuotationId = c.QuotationId,
        QuotationNo = c.Quotation?.No ?? string.Empty,
        CustomerName = c.Quotation?.Customer?.Name ?? string.Empty,
        ProjectName = c.Quotation?.ProjectName ?? string.Empty,
        PoDate = c.PoDate,
        Amount = c.Amount,
        HasAttachment = c.AttachmentPath is not null,
        SalesOrderId = soId,
        SalesOrderNo = soNo,
        CreatedAt = c.CreatedAt,
    };

    private static CustomerPoDto ToDto(CustomerPO c, Guid? soId, string? soNo) => new()
    {
        Id = c.Id,
        PoNo = c.PoNo,
        QuotationId = c.QuotationId,
        QuotationNo = c.Quotation?.No ?? string.Empty,
        CustomerName = c.Quotation?.Customer?.Name ?? string.Empty,
        ProjectName = c.Quotation?.ProjectName ?? string.Empty,
        PoDate = c.PoDate,
        Amount = c.Amount,
        HasAttachment = c.AttachmentPath is not null,
        SalesOrderId = soId,
        SalesOrderNo = soNo,
        Notes = c.Notes,
        AttachmentName = c.AttachmentName,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
    };

    public async Task<CustomerPoDto> UpdateNumberAsync(Guid customerPoId, string newPoNo, string? reason)
    {
        if (string.IsNullOrWhiteSpace(newPoNo))
            throw new ArgumentException("newPoNo is required.");

        var cpo = await _db.CustomerPOs.FindAsync(customerPoId)
            ?? throw new KeyNotFoundException("Customer PO tidak ditemukan.");

        if (cpo.PoNo == newPoNo)
            throw new InvalidOperationException("Nomor PO baru sama dengan nomor saat ini.");

        var old = cpo.PoNo;
        cpo.PoNo = newPoNo;
        cpo.UpdatedAt = DateTimeOffset.UtcNow;

        var history = new CustomerPoHistory
        {
            Id = Guid.NewGuid(),
            CustomerPoId = cpo.Id,
            OldPoNo = old,
            NewPoNo = newPoNo,
            ChangedAt = DateTime.UtcNow,
            Reason = reason,
        };

        // Attempt to capture current user from ambient context (CreatedBy/UpdatedBy pattern)
        try
        {
            // If HttpContext is available via synchronous access, get user info
            var httpContext = new HttpContextAccessor().HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var uid = httpContext.User.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "id")?.Value;
                if (Guid.TryParse(uid, out var g)) history.ChangedBy = g;
                history.ChangedByName = httpContext.User.Identity?.Name;
            }
        }
        catch
        {
            // ignore; fallback to nulls
        }

        _db.CustomerPoHistories.Add(history);
        await _db.SaveChangesAsync();

        var so = await _db.SalesOrders.FirstOrDefaultAsync(s => s.QuotationId == cpo.QuotationId && !s.IsDeleted);
        return ToDto(cpo, so?.Id, so?.No);
    }

    public async Task<IEnumerable<CustomerPoHistoryDto>> GetHistoryAsync(Guid customerPoId)
    {
        var items = await _db.CustomerPoHistories
            .Where(h => h.CustomerPoId == customerPoId)
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new CustomerPoHistoryDto
            {
                Id = h.Id,
                CustomerPoId = h.CustomerPoId,
                OldPoNo = h.OldPoNo,
                NewPoNo = h.NewPoNo,
                ChangedBy = h.ChangedBy,
                ChangedByName = h.ChangedByName,
                ChangedAt = h.ChangedAt,
                Reason = h.Reason,
            })
            .ToListAsync();

        return items;
    }
}
