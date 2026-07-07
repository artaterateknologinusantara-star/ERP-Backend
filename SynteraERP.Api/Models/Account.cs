using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public class Account : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public Guid? ParentAccountId { get; set; }
    public NormalBalanceType NormalBalance { get; set; }
    public bool IsControlAccount { get; set; } = false;

    public Account? ParentAccount { get; set; }
    public ICollection<Account> ChildAccounts { get; set; } = [];
}

public enum AccountType
{
    Asset,
    Liability,
    Equity,
    Revenue,
    Expense,
}

public enum NormalBalanceType
{
    Debit,
    Credit,
}
