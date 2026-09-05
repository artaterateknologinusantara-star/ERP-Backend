namespace SynteraERP.Api.Helpers;

// IDR tidak punya sub-unit (tidak ada "sen") - semua nilai uang dibulatkan ke 0 desimal di titik
// disimpan, bukan di-truncate ke decimal(18,2) yang cuma menghilangkan tampilan tapi tetap
// menyimpan pecahan. AwayFromZero dipilih (bukan default .NET Banker's Rounding/ToEven) supaya
// hasil pembulatan C# selalu sama dengan SQL Server ROUND(x, 0) yang pakai arithmetic rounding -
// draft data-fix dan kalkulasi service tidak boleh pernah berbeda untuk kasus x.50.
public static class MoneyMath
{
    public static decimal Round(decimal value) =>
        Math.Round(value, 0, MidpointRounding.AwayFromZero);
}
