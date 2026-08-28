using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Account;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class AccountService : IAccountService
{
    private readonly AppDbContext _db;

    public AccountService(AppDbContext db) => _db = db;

    public async Task<List<AccountDto>> GetTreeAsync()
    {
        var accounts = await _db.Accounts.AsNoTracking().OrderBy(x => x.Code).ToListAsync();
        var dtoById = accounts.ToDictionary(x => x.Id, ToDto);

        var roots = new List<AccountDto>();
        foreach (var account in accounts)
        {
            var dto = dtoById[account.Id];
            if (account.ParentAccountId.HasValue && dtoById.TryGetValue(account.ParentAccountId.Value, out var parent))
                parent.Children.Add(dto);
            else
                roots.Add(dto);
        }

        return roots;
    }

    public async Task<AccountDto?> GetByIdAsync(Guid id)
    {
        var x = await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
        return x is null ? null : ToDto(x);
    }

    public async Task<AccountDto> CreateAsync(CreateAccountRequest req)
    {
        var account = new Account
        {
            Code = req.Code,
            Name = req.Name,
            Type = ParseAccountType(req.Type),
            ParentAccountId = req.ParentAccountId,
            NormalBalance = ParseNormalBalance(req.NormalBalance),
            IsControlAccount = req.IsControlAccount,
        };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();
        return ToDto(account);
    }

    public async Task<AccountDto?> UpdateAsync(Guid id, UpdateAccountRequest req)
    {
        var account = await _db.Accounts.FindAsync(id);
        if (account is null) return null;

        account.Code             = req.Code;
        account.Name             = req.Name;
        account.Type             = ParseAccountType(req.Type);
        account.ParentAccountId  = req.ParentAccountId;
        account.NormalBalance    = ParseNormalBalance(req.NormalBalance);
        account.IsControlAccount = req.IsControlAccount;
        account.UpdatedAt        = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return ToDto(account);
    }

    private static AccountType ParseAccountType(string value) =>
        Enum.TryParse<AccountType>(value, true, out var parsed)
            ? parsed
            : throw new ArgumentException($"Account Type '{value}' tidak valid.");

    private static NormalBalanceType ParseNormalBalance(string value) =>
        Enum.TryParse<NormalBalanceType>(value, true, out var parsed)
            ? parsed
            : throw new ArgumentException($"NormalBalance '{value}' tidak valid.");

    private static AccountDto ToDto(Account x) => new()
    {
        Id               = x.Id,
        Code             = x.Code,
        Name             = x.Name,
        Type             = x.Type.ToString(),
        ParentAccountId  = x.ParentAccountId,
        NormalBalance    = x.NormalBalance.ToString(),
        IsControlAccount = x.IsControlAccount,
        CreatedAt        = x.CreatedAt,
    };
}
