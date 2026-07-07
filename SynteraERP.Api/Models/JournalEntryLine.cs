namespace SynteraERP.Api.Models;

public class JournalEntryLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JournalEntryId { get; set; }
    public Guid AccountId { get; set; }
    public decimal Debit { get; set; } = 0;
    public decimal Credit { get; set; } = 0;
    public string? Memo { get; set; }

    public JournalEntry JournalEntry { get; set; } = null!;
    public Account Account { get; set; } = null!;
}
