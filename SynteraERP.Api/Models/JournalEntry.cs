using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public class JournalEntry : BaseEntity
{
    public string EntryNumber { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public JournalSourceType SourceType { get; set; }
    public Guid? SourceId { get; set; }
    public JournalEntryStatus Status { get; set; } = JournalEntryStatus.Draft;
    public Guid? ReversedByEntryId { get; set; }
    public DateTimeOffset? PostedAt { get; set; }

    public JournalEntry? ReversedByEntry { get; set; }
    public ICollection<JournalEntryLine> Lines { get; set; } = [];
}

public enum JournalSourceType
{
    SalesInvoice,
    PurchaseInvoice,
    StockIn,
    StockOut,
    CashIn,
    CashOut,
    OperationalExpense,
    ManualAdjustment,
    Reversal,
}

public enum JournalEntryStatus
{
    Draft,
    Posted,
    Reversed,
}
