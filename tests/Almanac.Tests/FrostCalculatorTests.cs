namespace Almanac.Tests;

public class FrostCalculatorTests
{
    [Fact]
    public void Parse_ValidValue_ReturnsMonthAndDay()
    {
        Assert.Equal((10, 15), FrostCalculator.Parse("10-15"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("13-01")]
    [InlineData("10-32")]
    [InlineData("02-30")]
    [InlineData("1015")]
    [InlineData("Oct-15")]
    public void Parse_InvalidValue_ReturnsNull(string? value)
    {
        Assert.Null(FrostCalculator.Parse(value));
    }

    [Fact]
    public void Next_FirstFrostSooner_PicksFirstFrost()
    {
        var result = FrostCalculator.Next(new DateOnly(2026, 7, 19), (10, 7), (5, 5));

        Assert.NotNull(result);
        Assert.Equal("First frost", result.Value.Label);
        Assert.Equal(new DateOnly(2026, 10, 7), result.Value.Date);
        Assert.Equal(80, result.Value.DaysUntil);
    }

    [Fact]
    public void Next_FirstFrostPassed_WrapsToLastFrostNextSpring()
    {
        var result = FrostCalculator.Next(new DateOnly(2026, 11, 1), (10, 7), (5, 5));

        Assert.NotNull(result);
        Assert.Equal("Last frost", result.Value.Label);
        Assert.Equal(new DateOnly(2027, 5, 5), result.Value.Date);
    }

    [Fact]
    public void Next_EventIsToday_ReturnsZeroDays()
    {
        var result = FrostCalculator.Next(new DateOnly(2026, 10, 7), (10, 7), null);

        Assert.NotNull(result);
        Assert.Equal(0, result.Value.DaysUntil);
    }

    [Fact]
    public void Next_NothingConfigured_ReturnsNull()
    {
        Assert.Null(FrostCalculator.Next(new DateOnly(2026, 7, 19), null, null));
    }
}
