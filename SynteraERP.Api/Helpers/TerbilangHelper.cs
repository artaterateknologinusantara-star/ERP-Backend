namespace SynteraERP.Api.Helpers;

// Pure function, angka -> terbilang Bahasa Indonesia. Dipakai di PDF Quotation (Item D) dekat
// baris Grand Total pada halaman Recap Civil & ME.
public static class TerbilangHelper
{
    private static readonly string[] Satuan =
    [
        "", "Satu", "Dua", "Tiga", "Empat", "Lima", "Enam", "Tujuh", "Delapan", "Sembilan",
        "Sepuluh", "Sebelas",
    ];

    public static string ToWords(decimal amount)
    {
        var rounded = Math.Round(Math.Abs(amount), 0, MidpointRounding.AwayFromZero);
        var value = (long)rounded;

        var words = value == 0 ? "Nol" : Convert(value);
        var prefix = amount < 0 ? "Minus " : "";
        return $"{prefix}{words} Rupiah";
    }

    private static string Convert(long n)
    {
        if (n < 12) return Satuan[n];
        if (n < 20) return $"{Convert(n - 10)} Belas";
        if (n < 100) return CombineWithRemainder(n, 10, "Puluh");
        if (n < 200) return n == 100 ? "Seratus" : $"Seratus {Convert(n - 100)}";
        if (n < 1_000) return CombineWithRemainder(n, 100, "Ratus");
        if (n < 2_000) return n == 1_000 ? "Seribu" : $"Seribu {Convert(n - 1_000)}";
        if (n < 1_000_000) return CombineWithRemainder(n, 1_000, "Ribu");
        if (n < 1_000_000_000) return CombineWithRemainder(n, 1_000_000, "Juta");
        if (n < 1_000_000_000_000) return CombineWithRemainder(n, 1_000_000_000, "Miliar");
        return CombineWithRemainder(n, 1_000_000_000_000, "Triliun");
    }

    private static string CombineWithRemainder(long n, long unit, string unitName)
    {
        var quotient = n / unit;
        var remainder = n % unit;
        var head = $"{Convert(quotient)} {unitName}";
        return remainder == 0 ? head : $"{head} {Convert(remainder)}";
    }
}
