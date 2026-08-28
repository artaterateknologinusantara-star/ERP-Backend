using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Expense;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class ExpenseCategoryService : IExpenseCategoryService
{
    private readonly AppDbContext _db;

    public ExpenseCategoryService(AppDbContext db) => _db = db;

    public async Task<List<ExpenseCategoryDto>> ListAsync(bool? isActive = null)
    {
        var q = _db.ExpenseCategories.AsNoTracking().Include(x => x.Account).AsQueryable();

        if (isActive.HasValue)
            q = q.Where(x => x.IsActive == isActive.Value);

        var data = await q.OrderBy(x => x.Code).ToListAsync();
        return data.Select(ToDto).ToList();
    }

    public async Task<ExpenseCategoryDto?> GetByIdAsync(Guid id)
    {
        var cat = await _db.ExpenseCategories.AsNoTracking().Include(x => x.Account).FirstOrDefaultAsync(x => x.Id == id);
        return cat is null ? null : ToDto(cat);
    }

    public async Task<ExpenseCategoryDto> CreateAsync(CreateExpenseCategoryRequest request)
    {
        var accountExists = await _db.Accounts.AnyAsync(x => x.Id == request.AccountId && !x.IsDeleted);
        if (!accountExists)
            throw new InvalidOperationException("Akun COA tidak ditemukan.");

        var duplicateCode = await _db.ExpenseCategories.AnyAsync(x => x.Code == request.Code);
        if (duplicateCode)
            throw new InvalidOperationException($"Kode Expense Category '{request.Code}' sudah digunakan.");

        var cat = new ExpenseCategory
        {
            Code        = request.Code,
            Name        = request.Name,
            Description = request.Description,
            AccountId   = request.AccountId,
        };

        _db.ExpenseCategories.Add(cat);
        await _db.SaveChangesAsync();

        return (await GetByIdAsync(cat.Id))!;
    }

    public async Task<ExpenseCategoryDto?> UpdateAsync(Guid id, UpdateExpenseCategoryRequest request)
    {
        var cat = await _db.ExpenseCategories.FindAsync(id);
        if (cat is null) return null;

        var accountExists = await _db.Accounts.AnyAsync(x => x.Id == request.AccountId && !x.IsDeleted);
        if (!accountExists)
            throw new InvalidOperationException("Akun COA tidak ditemukan.");

        cat.Name        = request.Name;
        cat.Description = request.Description;
        cat.AccountId   = request.AccountId;
        cat.IsActive    = request.IsActive;
        cat.UpdatedAt   = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    private static ExpenseCategoryDto ToDto(ExpenseCategory x) => new()
    {
        Id          = x.Id,
        Code        = x.Code,
        Name        = x.Name,
        Description = x.Description,
        AccountId   = x.AccountId,
        AccountCode = x.Account?.Code ?? string.Empty,
        AccountName = x.Account?.Name ?? string.Empty,
        IsActive    = x.IsActive,
    };
}
