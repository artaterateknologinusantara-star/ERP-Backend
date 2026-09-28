using FluentAssertions;
using SynteraERP.Api.Helpers;
using Xunit;

namespace SynteraERP.Api.Tests;

public class TerbilangHelperTests
{
    [Theory]
    [InlineData(0, "Nol Rupiah")]
    [InlineData(1, "Satu Rupiah")]
    [InlineData(11, "Sebelas Rupiah")]
    [InlineData(12, "Dua Belas Rupiah")]
    [InlineData(20, "Dua Puluh Rupiah")]
    [InlineData(21, "Dua Puluh Satu Rupiah")]
    [InlineData(100, "Seratus Rupiah")]
    [InlineData(101, "Seratus Satu Rupiah")]
    [InlineData(150, "Seratus Lima Puluh Rupiah")]
    [InlineData(200, "Dua Ratus Rupiah")]
    [InlineData(1_000, "Seribu Rupiah")]
    [InlineData(1_500, "Seribu Lima Ratus Rupiah")]
    [InlineData(2_000, "Dua Ribu Rupiah")]
    [InlineData(1_000_000, "Satu Juta Rupiah")]
    [InlineData(1_000_000_000, "Satu Miliar Rupiah")]
    [InlineData(1_000_000_000_000, "Satu Triliun Rupiah")]
    [InlineData(105_660_000, "Seratus Lima Juta Enam Ratus Enam Puluh Ribu Rupiah")]
    public void ToWords_matches_expected_Indonesian_reading(decimal amount, string expected)
    {
        TerbilangHelper.ToWords(amount).Should().Be(expected);
    }

    [Fact]
    public void ToWords_rounds_fractional_rupiah_to_nearest_whole()
    {
        TerbilangHelper.ToWords(1000.60m).Should().Be("Seribu Satu Rupiah");
    }

    [Fact]
    public void ToWords_prefixes_negative_amounts_with_Minus()
    {
        TerbilangHelper.ToWords(-50).Should().Be("Minus Lima Puluh Rupiah");
    }
}
