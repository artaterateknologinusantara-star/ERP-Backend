using SynteraERP.Api.DTOs.Common;

namespace SynteraERP.Api.DTOs.JournalEntry;

public class JournalEntryLineDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Memo { get; set; }
}

public class JournalEntryDto
{
    public Guid Id { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public Guid? SourceId { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? ReversedByEntryId { get; set; }
    public DateTimeOffset? PostedAt { get; set; }
    public string? PostedByName { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public List<JournalEntryLineDto> Lines { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
}

public class JournalEntryListDto
{
    public Guid Id { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
}

public class JournalEntryQueryParams : PaginationParams
{
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public string? Status { get; set; }
    public string? SourceType { get; set; }
    public Guid? SourceId { get; set; }
}

public class CreateJournalEntryLineRequest
{
    public Guid AccountId { get; set; }
    public decimal Debit { get; set; } = 0;
    public decimal Credit { get; set; } = 0;
    public string? Memo { get; set; }
}

public class CreateJournalEntryRequest
{
    public DateTimeOffset? Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string SourceType { get; set; } = "ManualAdjustment";
    public Guid? SourceId { get; set; }
    public bool PostImmediately { get; set; } = true;
    public List<CreateJournalEntryLineRequest> Lines { get; set; } = [];
}

public class CreateOpeningBalanceRequest
{
    public DateTimeOffset? Date { get; set; }
    public string Description { get; set; } = "Opening Balance";
    public List<CreateJournalEntryLineRequest> Lines { get; set; } = [];
}

public class TrialBalanceRowDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal Balance { get; set; }
}
