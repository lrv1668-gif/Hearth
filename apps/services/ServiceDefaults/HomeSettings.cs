namespace ServiceDefaults;

/// <summary>
/// The household's location and time zone, set in Settings and stored by the Display
/// service (GET/PUT /display/home). Weather, Birds and Almanac read it through
/// <see cref="HomeLocationClient"/>. Every field is optional: coordinates come as a pair,
/// and a missing value falls back to the service's own env vars.
/// </summary>
/// <param name="Name">A display label for the place, e.g. "Denver, Colorado".</param>
/// <param name="TimeZone">An IANA zone id, e.g. "America/Denver".</param>
/// <param name="Units">"imperial" (°F, mph) or "metric" (°C, km/h) for the weather; null is imperial.</param>
public sealed record HomeSettings(
    double? Latitude, double? Longitude, string? Name, string? TimeZone, string? Units = null)
{
    public const int MaxNameLength = 100;

    public const string Imperial = "imperial";
    public const string Metric = "metric";

    /// <summary>Why the settings can't be saved, or null when they're valid.</summary>
    public string? Validate()
    {
        if (Latitude.HasValue != Longitude.HasValue) return "latitude and longitude go together";
        if (Latitude is < -90 or > 90 || Latitude is double.NaN) return "latitude must be -90–90";
        if (Longitude is < -180 or > 180 || Longitude is double.NaN) return "longitude must be -180–180";
        if (Name?.Length > MaxNameLength) return $"name must be at most {MaxNameLength} characters";
        if (!string.IsNullOrWhiteSpace(TimeZone) && !IsTimeZone(TimeZone)) return "time_zone must be an IANA time zone, e.g. America/Chicago";
        if (Units is not (null or Imperial or Metric)) return $"units must be {Imperial} or {Metric}";
        return null;
    }

    /// <summary>
    /// True for an IANA zone id this host's tz database knows. Windows ids ("Central
    /// Standard Time") resolve too but are refused — the renderer's browser only takes IANA.
    /// </summary>
    public static bool IsTimeZone(string id) =>
        !string.IsNullOrWhiteSpace(id) && TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone) && zone.HasIanaId;
}
