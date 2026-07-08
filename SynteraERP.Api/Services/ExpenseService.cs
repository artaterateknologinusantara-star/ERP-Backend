using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Expense;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class ExpenseService : IExpenseService
{
    private readonly AppDbContext _db;
    private readonly IJournalPostingService _journalPostingService;
    private readonly IWebHostEnvironment _env;

    public ExpenseService(AppDbContext db, IJournalPostingService journalPostingService, IWebHostEnvironment env)
    {
        _db = db;
        _journalPostingService = journalPostingService;
        _env = env;
    }

    // Transisi status Expense SENGAJA beda dari pola PurchaseRequest: Rejected di sini adalah dead-end
    // (tidak bisa balik ke Draft). Expense yang di-approve punya efek langsung ke Kas/Bank, jadi Expense
    // yang ditolak harus dibuat ulang sebagai entry baru supaya jejak audit "kenapa ditolak" tetap bersih
    // dan tidak tercampur dengan revisi berikutnya.
    private static readonly Dictionary<ExpenseStatus, List<ExpenseStatus>> ValidTransitions = new()
    {
        [ExpenseStatus.Draft]     = [ExpenseStatus.Submitted],
        [ExpenseStatus.Submitted] = [ExpenseStatus.Approved, ExpenseStatus.Rejected],
        [ExpenseStatus.Approved]  = [ExpenseStatus.Paid],
        [ExpenseStatus.Rejected]  = [],
    };

    public async Task<PaginatedResponse<ExpenseListDto>> ListAsync(ExpenseQueryParams p)
    {
        var q = _db.Expenses
            .Include(x => x.ExpenseCategory)
            .Include(x => x.Vendor)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(p.Status) && Enum.TryParse<ExpenseStatus>(p.Status, true, out var status))
            q = q.Where(x => x.Status == status);

        if (p.ExpenseCategoryId.HasValue)
            q = q.Where(x => x.ExpenseCategoryId == p.ExpenseCategoryId.Value);

        if (p.DateFrom.HasValue)
            q = q.Where(x => x.ExpenseDate >= p.DateFrom.Value);

        if (p.DateTo.HasValue)
            q = q.Where(x => x.ExpenseDate <= p.DateTo.Value);

        q = q.OrderByDescending(x => x.CreatedAt);

        var total = await q.CountAsync();
        var data = await q.Skip(p.Skip).Take(p.PerPage).ToListAsync();
        var mapped = data.Select(ToListDto).ToList();

        return PaginatedResponse<ExpenseListDto>.Create(mapped, total, p.Page, p.PerPage);
    }

    public async Task<ExpenseDto?> GetByIdAsync(Guid id)
    {
        var exp = await _db.Expenses
            .Include(x => x.ExpenseCategory)
            .Include(x => x.Vendor)
            .Include(x => x.CashBankAccount)
            .FirstOrDefaultAsync(x => x.Id == id);

        return exp is null ? null : await ToDtoAsync(exp);
    }

    public async Task<ExpenseDto> CreateAsync(CreateExpenseRequest request, IFormFile? attachment)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("Amount harus lebih dari 0.");

        var category = await _db.ExpenseCategories.FirstOrDefaultAsync(x => x.Id == request.ExpenseCategoryId)
            ?? throw new KeyNotFoundException("Expense Category tidak ditemukan.");

        if (!category.IsActive)
            throw new InvalidOperationException($"Expense Category '{category.Name}' sudah tidak aktif.");

        Guid cashBankAccountId;
        if (request.CashBankAccountId.HasValue)
        {
            var accountExists = await _db.Accounts.AnyAsync(x => x.Id == request.CashBankAccountId.Value && !x.IsDeleted);
            if (!accountExists)
                throw new InvalidOperationException("Akun Kas/Bank tidak ditemukan.");
            cashBankAccountId = request.CashBankAccountId.Value;
        }
        else
        {
            // Default akun Kas/Bank ke "1-1001 Kas", mengikuti keputusan Fase 2.
            cashBankAccountId = await _db.Accounts
                .Where(x => x.Code == "1-1001" && !x.IsDeleted)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            if (cashBankAccountId == Guid.Empty)
                throw new InvalidOperationException("Akun default Kas (1-1001) tidak ditemukan di Chart of Accounts.");
        }

        if (request.VendorId.HasValue)
        {
            var vendorExists = await _db.Suppliers.AnyAsync(x => x.Id == request.VendorId.Value && !x.IsDeleted);
            if (!vendorExists)
                throw new InvalidOperationException("Vendor tidak ditemukan.");
        }

        string? attachmentPath = null;
        string? attachmentName = null;

        if (attachment is { Length: > 0 })
        {
            var uploadsDir = Path.Combine(_env.ContentRootPath, "uploads", "expense");
            Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(attachment.FileName);
            var storedName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploadsDir, storedName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await attachment.CopyToAsync(stream);

            attachmentPath = Path.Combine("expense", storedName);
            attachmentName = attachment.FileName;
        }

        var expenseNo = await NextNumberAsync();

        var expense = new Models.Expense
        {
            ExpenseNo         = expenseNo,
            ExpenseDate       = request.ExpenseDate,
            ExpenseCategoryId = request.ExpenseCategoryId,
            Description       = request.Description,
            VendorId          = request.VendorId,
            Amount            = request.Amount,
            Method            = request.Method,
            CashBankAccountId = cashBankAccountId,
            ReferenceNumber   = request.ReferenceNumber,
            AttachmentPath    = attachmentPath,
            AttachmentName    = attachmentName,
            Status            = ExpenseStatus.Draft,
            Remarks           = request.Remarks,
        };

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();

        return (await GetByIdAsync(expense.Id))!;
    }

    public async Task<ExpenseDto?> SubmitAsync(Guid id)
    {
        var expense = await _db.Expenses.FindAsync(id);
        if (expense is null) return null;

        EnsureTransition(expense.Status, ExpenseStatus.Submitted);

        expense.Status    = ExpenseStatus.Submitted;
        expense.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<ExpenseDto?> ApproveAsync(Guid id, Guid? userId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var expense = await _db.Expenses
            .Include(x => x.ExpenseCategory).ThenInclude(c => c.Account)
            .Include(x => x.CashBankAccount)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (expense is null) return null;

        EnsureTransition(expense.Status, ExpenseStatus.Approved);

        expense.Status     = ExpenseStatus.Approved;
        expense.ApprovedAt = DateTimeOffset.UtcNow;
        expense.ApprovedBy = userId;
        expense.UpdatedAt  = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

        await _journalPostingService.PostAsync(
            $"Expense {expense.ExpenseNo} ({expense.ExpenseCategory.Name}) - {expense.Description}",
            JournalSourceType.OperationalExpense,
            expense.Id,
            DateTimeOffset.UtcNow,
            new PostingLine[]
            {
                new(expense.ExpenseCategory.Account.Code, expense.Amount, 0, expense.ExpenseCategory.Name),
                new(expense.CashBankAccount.Code, 0, expense.Amount, "Pembayaran Expense"),
            });

        await tx.CommitAsync();

        return (await GetByIdAsync(id))!;
    }

    public async Task<ExpenseDto?> RejectAsync(Guid id, string? reason)
    {
        var expense = await _db.Expenses.FindAsync(id);
        if (expense is null) return null;

        EnsureTransition(expense.Status, ExpenseStatus.Rejected);

        expense.Status    = ExpenseStatus.Rejected;
        expense.UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(reason))
            expense.Remarks = string.IsNullOrWhiteSpace(expense.Remarks)
                ? $"Ditolak: {reason}"
                : $"{expense.Remarks}\nDitolak: {reason}";

        await _db.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<(byte[] data, string contentType, string fileName)?> GetAttachmentAsync(Guid id)
    {
        var exp = await _db.Expenses.FindAsync(id);
        if (exp is null || exp.AttachmentPath is null) return null;

        var fullPath = Path.Combine(_env.ContentRootPath, "uploads", exp.AttachmentPath);
        if (!File.Exists(fullPath)) return null;

        var data = await File.ReadAllBytesAsync(fullPath);
        var contentType = GetContentType(exp.AttachmentPath);
        var fileName = exp.AttachmentName ?? Path.GetFileName(exp.AttachmentPath);
        return (data, contentType, fileName);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static void EnsureTransition(ExpenseStatus from, ExpenseStatus to)
    {
        if (!ValidTransitions.TryGetValue(from, out var allowed) || !allowed.Contains(to))
            throw new InvalidOperationException($"Tidak bisa mengubah status Expense dari {from} ke {to}.");
    }

    private async Task<string> NextNumberAsync()
    {
        var config = await _db.NumberingConfigs
            .FirstOrDefaultAsync(n => n.DocType == "EXPENSE")
            ?? throw new InvalidOperationException("NumberingConfig for EXPENSE not found");

        var no = config.GenerateNext();
        await _db.SaveChangesAsync();
        return no;
    }

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

    private static ExpenseListDto ToListDto(Models.Expense x) => new()
    {
        Id                  = x.Id,
        ExpenseNo           = x.ExpenseNo,
        ExpenseDate         = x.ExpenseDate,
        ExpenseCategoryId   = x.ExpenseCategoryId,
        ExpenseCategoryName = x.ExpenseCategory?.Name ?? string.Empty,
        Description         = x.Description,
        VendorName          = x.Vendor?.Name,
        Amount              = x.Amount,
        Status              = x.Status.ToString(),
    };

    private async Task<ExpenseDto> ToDtoAsync(Models.Expense x)
    {
        string? approvedByName = null;
        if (x.ApprovedBy.HasValue)
            approvedByName = await _db.Users
                .Where(u => u.Id == x.ApprovedBy.Value)
                .Select(u => u.Name)
                .FirstOrDefaultAsync();

        return new ExpenseDto
        {
            Id                  = x.Id,
            ExpenseNo           = x.ExpenseNo,
            ExpenseDate         = x.ExpenseDate,
            ExpenseCategoryId   = x.ExpenseCategoryId,
            ExpenseCategoryName = x.ExpenseCategory?.Name ?? string.Empty,
            Description         = x.Description,
            VendorId            = x.VendorId,
            VendorName          = x.Vendor?.Name,
            Amount              = x.Amount,
            Method              = x.Method,
            CashBankAccountId   = x.CashBankAccountId,
            CashBankAccountCode = x.CashBankAccount?.Code ?? string.Empty,
            CashBankAccountName = x.CashBankAccount?.Name ?? string.Empty,
            ReferenceNumber     = x.ReferenceNumber,
            HasAttachment       = x.AttachmentPath is not null,
            AttachmentName      = x.AttachmentName,
            Status              = x.Status.ToString(),
            ApprovedAt          = x.ApprovedAt,
            ApprovedByName      = approvedByName,
            Remarks             = x.Remarks,
            CreatedAt           = x.CreatedAt,
        };
    }
}
