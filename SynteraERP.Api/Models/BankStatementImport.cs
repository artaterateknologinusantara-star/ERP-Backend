using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public class BankStatementImport : BaseEntity
{
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public DateTimeOffset ImportDate { get; set; }
    public string FileName { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal? StatementEndingBalance { get; set; }

    public ICollection<BankStatementLine> Lines { get; set; } = new List<BankStatementLine>();
}

public enum BankStatementLineMatchStatus
{
    Unmatched,
    Matched,
    Ignored,
}

public class BankStatementLine : BaseEntity
{
    public Guid BankStatementImportId { get; set; }
    public BankStatementImport Import { get; set; } = null!;
    public Guid AccountId { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string Description { get; set; } = null!;
    public string? ReferenceNumber { get; set; }
    public decimal Amount { get; set; }
    public BankStatementLineMatchStatus MatchStatus { get; set; } = BankStatementLineMatchStatus.Unmatched;
    public Guid? MatchedJournalEntryLineId { get; set; }
}
