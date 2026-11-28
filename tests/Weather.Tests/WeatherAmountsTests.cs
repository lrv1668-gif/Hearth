namespace Weather.Tests;

public sealed class WeatherAmountsTests
{
    [Theory]
    [InlineData(null,  null)]
    [InlineData(0.0,   null)]
    [InlineData(0.04,  null)]
    [InlineData(0.05,  "0.1 in")]
    [InlineData(0.34,  "0.3 in")]
    [InlineData(1.25,  "1.3 in")]
    public void Rain_Imperial_RoundsToTenths(double? amount, string? expected) =>
        Assert.Equal(expected, WeatherAmounts.Rain(amount, metric: false));

    [Theory]
    [InlineData(0.9,  null)]
    [InlineData(1.0,  "1 mm")]
    [InlineData(4.5,  "5 mm")]
    public void Rain_Metric_RoundsToWholeMillimetres(double amount, string? expected) =>
        Assert.Equal(expected, WeatherAmounts.Rain(amount, metric: true));

    [Theory]
    [InlineData(0.05, null)]
    [InlineData(0.17, "0.2 in")]
    [InlineData(0.5, "0.5 in")]
    [InlineData(0.96, "1.0 in")]
    [InlineData(2.6, "3 in")]
    public void Snow_Imperial_TenthsUnderAnInchThenWhole(double amount, string? expected) =>
        Assert.Equal(expected, WeatherAmounts.Snow(amount, metric: false));

    [Theory]
    [InlineData(0.4, null)]
    [InlineData(0.5, "1 cm")]
    [InlineData(6.5, "7 cm")]
    public void Snow_Metric_RoundsToWholeCentimetres(double amount, string? expected) =>
        Assert.Equal(expected, WeatherAmounts.Snow(amount, metric: true));

    [Fact]
    public void Expected_SnowEnoughToSay_WinsOverItsMeltedTotal() =>
        Assert.Equal("3 in snow", WeatherAmounts.Expected(0.3, 2.6, metric: false));

    [Fact]
    public void Expected_TraceOfSnow_FallsBackToRain() =>
        Assert.Equal("0.3 in", WeatherAmounts.Expected(0.3, 0.05, metric: false));

    [Fact]
    public void Expected_LightSnow_NamesTheSnowNotItsMeltedTotal() =>
        Assert.Equal("0.2 in snow", WeatherAmounts.Expected(0.083, 0.165, metric: false));

    [Fact]
    public void Expected_Dry_ReturnsNull() =>
        Assert.Null(WeatherAmounts.Expected(0, 0, metric: true));
}
