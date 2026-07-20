using Xunit;

namespace Photos.Tests;

public sealed class SeasonalQueryTests
{
    [Fact]
    public void Expand_QueryWithoutToken_ReturnsQueryUnchanged()
    {
        var result = SeasonalQuery.Expand("nature,architecture", new DateOnly(2026, 7, 19), isNorthern: true);

        Assert.Equal("nature,architecture", result);
    }

    [Theory]
    [InlineData(1, "winter snow")]
    [InlineData(4, "spring blossoms")]
    [InlineData(7, "summer nature")]
    [InlineData(10, "autumn leaves")]
    [InlineData(12, "winter snow")]
    public void Expand_SeasonalTokenNorthernHemisphere_MapsMonthToSeasonTerm(int month, string expected)
    {
        var result = SeasonalQuery.Expand("seasonal", new DateOnly(2026, month, 15), isNorthern: true);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1, "summer nature")]
    [InlineData(4, "autumn leaves")]
    [InlineData(7, "winter snow")]
    [InlineData(10, "spring blossoms")]
    public void Expand_SeasonalTokenSouthernHemisphere_FlipsSeasons(int month, string expected)
    {
        var result = SeasonalQuery.Expand("seasonal", new DateOnly(2026, month, 15), isNorthern: false);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Expand_TokenMixedWithOtherCategories_ReplacesOnlyToken()
    {
        var result = SeasonalQuery.Expand("nature,seasonal,abstract", new DateOnly(2026, 1, 15), isNorthern: true);

        Assert.Equal("nature,winter snow,abstract", result);
    }

    [Fact]
    public void Expand_TokenUppercase_IsReplacedCaseInsensitively()
    {
        var result = SeasonalQuery.Expand("Seasonal", new DateOnly(2026, 7, 15), isNorthern: true);

        Assert.Equal("summer nature", result);
    }
}
