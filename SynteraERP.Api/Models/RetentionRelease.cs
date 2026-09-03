using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public class RetentionRelease : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public DateTimeOffset ReleaseDate { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}
