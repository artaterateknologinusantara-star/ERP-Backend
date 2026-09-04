namespace SynteraERP.Api.DTOs.BankReconciliation;

public class ImportBankStatementRequest
{
    public Guid AccountId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal? StatementEndingBalance { get; set; }
}

public class CsvRowError
{
    public int RowNumber { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class BankStatementImportSummaryDto
{
    public Guid Id { get; set; }
    public int LineCount { get; set; }
}

public class BankStatementImportResult
{
    public bool Success { get; set; }
    public BankStatementImportSummaryDto? Summary { get; set; }
    public List<CsvRowError>? RowErrors { get; set; }
}

public class BankStatementImportListDto
{
    public Guid Id { get; set; }
    public DateTimeOffset ImportDate { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal? StatementEndingBalance { get; set; }
    public int LineCount { get; set; }
    public int MatchedCount { get; set; }
    public int UnmatchedCount { get; set; }
    public int IgnoredCount { get; set; }
}

public class MatchCandidateDto
{
    public Guid JournalEntryLineId { get; set; }
    public Guid JournalEntryId { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

public class BankStatementLineDetailDto
{
    public Guid Id { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public decimal Amount { get; set; }
    public string MatchStatus { get; set; } = string.Empty;
    public Guid? MatchedJournalEntryLineId { get; set; }
    public List<MatchCandidateDto> SuggestedMatches { get; set; } = new();
}

public class BankStatementImportDetailDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public DateTimeOffset ImportDate { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal? StatementEndingBalance { get; set; }
    public List<BankStatementLineDetailDto> Lines { get; set; } = new();
}

public class MatchLineRequest
{
    public Guid JournalEntryLineId { get; set; }
}

public class AccountBalanceDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}
