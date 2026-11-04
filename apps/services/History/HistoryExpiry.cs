namespace History;

public static class HistoryExpiry
{
    /// <summary>Today's date in <paramref name="zone"/> — the day "on this day" is for.</summary>
    public static DateOnly Today(DateTimeOffset utcNow, TimeZoneInfo zone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utcNow, zone).DateTime);

    /// <summary>The next local midnight in <paramref name="zone"/>, when the day's events change.</summary>
    public static DateTimeOffset NextMidnight(DateTimeOffset utcNow, TimeZoneInfo zone)
    {
        var tomorrow = Today(utcNow, zone).AddDays(1).ToDateTime(TimeOnly.MinValue);
        // A zone that skips midnight (a DST jump) starts the day at the first valid minute
        while (zone.IsInvalidTime(tomorrow)) tomorrow = tomorrow.AddMinutes(1);
        return new DateTimeOffset(tomorrow, zone.GetUtcOffset(tomorrow)).ToUniversalTime();
    }

    /// <summary>The household's zone, or UTC when it isn't set or isn't known here.</summary>
    public static TimeZoneInfo Zone(string? id) =>
        id is { Length: > 0 } && TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone) ? zone : TimeZoneInfo.Utc;
}
