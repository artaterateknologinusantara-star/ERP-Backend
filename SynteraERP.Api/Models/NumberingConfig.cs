namespace SynteraERP.Api.Models;

public class NumberingConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DocType { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public int LastNumber { get; set; } = 0;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string GenerateNext()
    {
        LastNumber++;
        var year = DateTime.UtcNow.ToString("yy");
        return $"{Prefix}-{year}.{LastNumber:D4}";
    }
}
