namespace SynteraERP.Api.Models;

public class SalesOrderTermin
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SalesOrderId { get; set; }
    public int SortOrder { get; set; } = 0;
    public string Description { get; set; } = string.Empty;
    public decimal Percentage { get; set; }

    public SalesOrder SalesOrder { get; set; } = null!;
}
