using System.Globalization;

namespace Data.Abstractions;

public static class CacheFreshness
{
    /// <summary>
    /// True when <paramref name="fetchedAtIso"/> is missing, unparseable, or older than <paramref name="maxAge"/>.
    /// </summary>
    /// <remarks>
    /// Parses with <see cref="DateTimeStyles.RoundtripKind"/> so a stored UTC ("...Z") timestamp keeps
    /// Kind=Utc. Default <see cref="DateTime.TryParse(string?, out DateTime)"/> converts it to local time,
    /// which skews the comparison against <see cref="DateTime.UtcNow"/> by the machine's UTC offset
    /// (the cache would read as perpetually stale).
    /// </remarks>
    public static bool IsStale(string? fetchedAtIso, TimeSpan maxAge)
    {
        if (fetchedAtIso is null) return true;
        if (!DateTime.TryParse(fetchedAtIso, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var fetched)) return true;
        return (DateTime.UtcNow - fetched) > maxAge;
    }
}
