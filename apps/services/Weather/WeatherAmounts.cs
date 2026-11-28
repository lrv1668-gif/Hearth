using System.Globalization;

namespace Weather;

/// <summary>
/// Expected rain and snow in words, in the snapshot's own units, so every screen says it the
/// same way. Amounts under a threshold say nothing: a trace isn't worth a line.
/// </summary>
public static class WeatherAmounts
{
    /// <summary>The least rain (liquid) worth saying: 0.05 in or 1 mm.</summary>
    public static double RainShown(bool metric) => metric ? 1 : 0.05;
    /// <summary>The least snow (depth) worth saying: a tenth of an inch or half a centimetre.
    /// Low, because a dusting is still news, and below it a snow day would read as its
    /// melted total.</summary>
    public static double SnowShown(bool metric) => metric ? 0.5 : 0.1;
    /// <summary>A day's rain that's heavy: an inch or 25 mm.</summary>
    public static double HeavyRain(bool metric) => metric ? 25 : 1;

    /// <summary>"0.3 in" or "4 mm" of rain, or null under <see cref="RainShown"/>.</summary>
    public static string? Rain(double? amount, bool metric) =>
        amount is not { } value || value < RainShown(metric) ? null
        : metric ? $"{Whole(value)} {WeatherUnits.Millimetres}"
        : $"{Math.Round(value, 1, MidpointRounding.AwayFromZero).ToString("0.0", CultureInfo.InvariantCulture)} {WeatherUnits.Inches}";

    /// <summary>"2 in" or "5 cm" of snow (under an inch to the tenth), or null under <see cref="SnowShown"/>.</summary>
    public static string? Snow(double? amount, bool metric) =>
        amount is not { } value || value < SnowShown(metric) ? null
        : metric ? $"{Whole(value)} {WeatherUnits.Centimetres}"
        : value < 1 ? $"{Math.Round(value, 1, MidpointRounding.AwayFromZero).ToString("0.0", CultureInfo.InvariantCulture)} {WeatherUnits.Inches}"
        : $"{Whole(value)} {WeatherUnits.Inches}";

    /// <summary>What a day is expected to bring: its snow when there's enough to say
    /// ("2 in snow"), else its rain ("0.3 in"), else null. Open-Meteo's precipitation is
    /// the liquid total, snow melted included, so snow wins when both are there.</summary>
    public static string? Expected(double? precipitation, double? snowfall, bool metric) =>
        Snow(snowfall, metric) is { } snow ? $"{snow} snow" : Rain(precipitation, metric);

    private static string Whole(double value) =>
        Math.Round(value, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture);
}
