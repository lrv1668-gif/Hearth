using Weather.Records;

namespace Weather.Tests;

public sealed class WeatherAlertsTests
{
    private static readonly CurrentWeatherResponse Calm =
        new(60, 1, "Mostly clear", 5, "2026-11-21T12:00:00Z", UvIndex: 2, UsAqi: 30);

    private static ForecastDayResponse Day(
        double max = 60, double min = 45, int code = 1, int? chance = null, double? uv = 3, double? wind = 10) =>
        new("2026-11-21", code, "Mostly clear", max, min, "2026-11-21T06:50", "2026-11-21T16:40", uv, chance, wind);

    private static List<WeatherAlert> For(CurrentWeatherResponse current, params ForecastDayResponse[] days) =>
        WeatherAlerts.For(new WeatherSnapshot(current, [.. days]));

    [Fact]
    public void For_CalmDay_ReturnsNone() =>
        Assert.Empty(For(Calm, Day(), Day()));

    [Fact]
    public void For_ThunderstormCode_AlertsStorm() =>
        Assert.Equal("storm", Assert.Single(For(Calm, Day(code: 95), Day())).Kind);

    [Theory]
    [InlineData(null)]
    [InlineData(70)]
    public void For_SnowLikelyOrUnknownChance_AlertsSnow(int? chance) =>
        Assert.Equal("Snow likely today", Assert.Single(For(Calm, Day(code: 73, chance: chance), Day())).Text);

    [Fact]
    public void For_SnowUnlikely_ReturnsNone() =>
        Assert.Empty(For(Calm, Day(code: 73, chance: 20), Day()));

    [Fact]
    public void For_FreezingTonightImperial_AlertsFrostWithLow() =>
        Assert.Equal("Frost tonight · low of 28°", Assert.Single(For(Calm, Day(), Day(min: 28.4))).Text);

    [Fact]
    public void For_JustAboveFreezingImperial_ReturnsNone() =>
        Assert.Empty(For(Calm, Day(), Day(min: 33)));

    [Fact]
    public void For_Metric_UsesCelsiusThresholds()
    {
        var metric = Calm with { Temperature = 15, TemperatureUnit = WeatherUnits.Celsius, WindSpeed = 8, WindUnit = WeatherUnits.Kmh };

        // 2 °C is above freezing in Celsius, though it would be below 32 if read as °F
        Assert.Empty(For(metric, Day(max: 20, min: 5), Day(max: 18, min: 2)));
        Assert.Equal("frost", Assert.Single(For(metric, Day(max: 20, min: 5), Day(max: 10, min: -1))).Kind);
        Assert.Equal("heat", Assert.Single(For(metric, Day(max: 36, min: 20), Day(max: 30, min: 18))).Kind);
    }

    [Fact]
    public void For_PeakWindAtThreshold_AlertsWindInItsUnit() =>
        Assert.Equal("Windy today · up to 25 mph", Assert.Single(For(Calm, Day(wind: 25.2), Day())).Text);

    [Fact]
    public void For_GustyNowButCalmDay_ReturnsNone() =>
        Assert.Empty(For(Calm with { WindSpeed = 40 }, Day(wind: 12), Day()));

    [Fact]
    public void For_PoorAir_AlertsWithCategory() =>
        Assert.Equal("Air quality: unhealthy", Assert.Single(For(Calm with { UsAqi = 160 }, Day(), Day())).Text);

    [Fact]
    public void For_VeryHighUv_AlertsPeak() =>
        Assert.Equal("Very high UV today · peak 9", Assert.Single(For(Calm, Day(uv: 8.6), Day())).Text);

    [Fact]
    public void For_SeveralAtOnce_MostPressingFirst()
    {
        var alerts = For(Calm, Day(code: 95, wind: 30), Day(min: 20));

        Assert.Equal(["storm", "frost", "wind"], alerts.Select(a => a.Kind));
    }

    [Fact]
    public void For_NoForecast_StillChecksCurrent() =>
        Assert.Equal("air", Assert.Single(For(Calm with { UsAqi = 120 })).Kind);
}
