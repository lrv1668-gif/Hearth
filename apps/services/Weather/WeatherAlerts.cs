using System.Globalization;
using Weather.Records;

namespace Weather;

/// <summary>
/// Picks the alerts a snapshot calls for, most pressing first. Derived on each request
/// rather than cached, like the categories, so they always match the snapshot. The
/// thresholds sit in the snapshot's own units, so a metric household gets the same
/// alerts at the same weather.
/// </summary>
public static class WeatherAlerts
{
    // WMO weather codes (see WeatherFetcher's table)
    private static readonly int[] Storms = [95, 96, 99];
    private static readonly int[] Snow = [71, 73, 75, 77, 85, 86];
    private static readonly int[] FreezingRain = [56, 57, 66, 67];

    /// <summary>The chance of precipitation at which snow is worth a line.</summary>
    public const int SnowChance = 50;
    /// <summary>US AQI above this is unhealthy for sensitive groups.</summary>
    public const int PoorAir = 100;
    /// <summary>The peak UV index that's "Very high" (WHO).</summary>
    public const double HighUv = 8;

    public static List<WeatherAlert> For(WeatherSnapshot snapshot)
    {
        var (current, forecast) = snapshot;
        var metric = current.TemperatureUnit == WeatherUnits.Celsius;
        var today = forecast.Count > 0 ? forecast[0] : null;
        // Tonight's low is the next day's minimum, which falls around dawn.
        var tonight = forecast.Count > 1 ? forecast[1] : null;
        var alerts = new List<WeatherAlert>();

        if (today is not null && Storms.Contains(today.WeatherCode))
            alerts.Add(new("storm", "Thunderstorms today"));
        if (today is not null && FreezingRain.Contains(today.WeatherCode))
            alerts.Add(new("snow", "Freezing rain today"));
        else if (today is not null && Snow.Contains(today.WeatherCode) && today.PrecipitationChance is not < SnowChance)
            alerts.Add(new("snow", WeatherAmounts.Snow(today.SnowfallSum, metric) is { } depth
                ? $"Snow likely today · {depth} expected"
                : "Snow likely today"));
        // An inch or 25 mm in a day floods gutters and roads; a snow day already has its line.
        else if (today?.PrecipitationSum is { } rain && rain >= WeatherAmounts.HeavyRain(metric))
            alerts.Add(new("rain", $"Heavy rain today · {WeatherAmounts.Rain(rain, metric)} expected"));

        var freezing = metric ? 0 : 32;
        if (tonight is not null && tonight.TempMin <= freezing)
            alerts.Add(new("frost", $"Frost tonight · low of {Degrees(tonight.TempMin)}"));

        var hot = metric ? 35 : 95;
        if (today is not null && today.TempMax >= hot)
            alerts.Add(new("heat", $"Hot today · high of {Degrees(today.TempMax)}"));

        // The day's peak rather than the reading now, so the alert stays true between the
        // frame's redraws and doesn't come and go as gusts cross the line.
        var windy = metric ? 40 : 25;
        if (today?.WindSpeedMax is { } peak && peak >= windy)
            alerts.Add(new("wind", $"Windy today · up to {Math.Round(peak).ToString(CultureInfo.InvariantCulture)} {current.WindUnit}"));

        if (current.UsAqi is > PoorAir)
            alerts.Add(new("air", $"Air quality: {current.AqiCategory!.ToLowerInvariant()}"));

        if (today?.UvIndexMax is >= HighUv)
            alerts.Add(new("uv", $"{today.UvMaxCategory} UV today · peak {Math.Round(today.UvIndexMax.Value).ToString(CultureInfo.InvariantCulture)}"));

        return alerts;
    }

    private static string Degrees(double value) =>
        Math.Round(value).ToString(CultureInfo.InvariantCulture) + "°";
}
