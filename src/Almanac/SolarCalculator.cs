namespace Almanac;

public record SunTimes(DateTime Sunrise, DateTime Sunset, double DayLengthMinutes);

/// <summary>
/// NOAA solar-position algorithm ("General Solar Position Calculations", NOAA GML).
/// Accurate to within ~2 minutes for latitudes below the polar circles.
/// </summary>
public static class SolarCalculator
{
    private const double DegToRad = Math.PI / 180.0;
    private const double RadToDeg = 180.0 / Math.PI;

    // Sunrise/sunset zenith: 90° + 50 arcminutes for refraction and solar radius.
    private const double ZenithDeg = 90.833;

    /// <summary>
    /// Sunrise and sunset for <paramref name="date"/> as wall-clock times in
    /// <paramref name="tz"/>. Longitude is positive east. Returns null during
    /// polar day or polar night.
    /// </summary>
    public static SunTimes? SunTimesFor(DateOnly date, double latitude, double longitude, TimeZoneInfo tz)
    {
        var dayLength = DayLengthMinutes(date, latitude);
        if (dayLength is null) return null;

        var gamma = FractionalYear(date);
        var eqTimeMin = 229.18 * (0.000075
            + 0.001868 * Math.Cos(gamma) - 0.032077 * Math.Sin(gamma)
            - 0.014615 * Math.Cos(2 * gamma) - 0.040849 * Math.Sin(2 * gamma));

        var haDeg = dayLength.Value / 8.0;
        var sunriseUtcMin = 720 - 4 * (longitude + haDeg) - eqTimeMin;
        var sunsetUtcMin = 720 - 4 * (longitude - haDeg) - eqTimeMin;

        var midnightUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var sunrise = TimeZoneInfo.ConvertTimeFromUtc(midnightUtc.AddMinutes(sunriseUtcMin), tz);
        var sunset = TimeZoneInfo.ConvertTimeFromUtc(midnightUtc.AddMinutes(sunsetUtcMin), tz);

        return new SunTimes(sunrise, sunset, dayLength.Value);
    }

    /// <summary>Day length in minutes, or null during polar day/night.</summary>
    public static double? DayLengthMinutes(DateOnly date, double latitude)
    {
        var gamma = FractionalYear(date);
        var decl = 0.006918
            - 0.399912 * Math.Cos(gamma) + 0.070257 * Math.Sin(gamma)
            - 0.006758 * Math.Cos(2 * gamma) + 0.000907 * Math.Sin(2 * gamma)
            - 0.002697 * Math.Cos(3 * gamma) + 0.00148 * Math.Sin(3 * gamma);

        var latRad = latitude * DegToRad;
        var cosHa = Math.Cos(ZenithDeg * DegToRad) / (Math.Cos(latRad) * Math.Cos(decl))
            - Math.Tan(latRad) * Math.Tan(decl);
        if (cosHa is > 1 or < -1) return null;

        var haDeg = Math.Acos(cosHa) * RadToDeg;

        // The sun crosses 1° of hour angle in 4 minutes; day spans 2 × haDeg.
        return 8 * haDeg;
    }

    private static double FractionalYear(DateOnly date)
    {
        var daysInYear = DateTime.IsLeapYear(date.Year) ? 366.0 : 365.0;
        return 2 * Math.PI / daysInYear * (date.DayOfYear - 1 + 0.5);
    }
}
