using System.Globalization;

namespace Almanac;

public static class FrostCalculator
{
    /// <summary>Parses an "MM-DD" env value; null when unset or malformed.</summary>
    public static (int Month, int Day)? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var parts = value.Split('-');
        if (parts.Length != 2
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var month)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var day)
            || month is < 1 or > 12
            || day < 1 || day > DateTime.DaysInMonth(2000, month))
            return null;
        return (month, day);
    }

    /// <summary>
    /// The next upcoming frost event from today: whichever of first/last frost
    /// comes sooner in the annual cycle. Null when neither date is configured.
    /// </summary>
    public static (string Label, DateOnly Date, int DaysUntil)? Next(
        DateOnly today, (int Month, int Day)? firstFrost, (int Month, int Day)? lastFrost)
    {
        var candidates = new List<(string Label, DateOnly Date)>();
        if (firstFrost is { } ff) candidates.Add(("First frost", NextOccurrence(today, ff.Month, ff.Day)));
        if (lastFrost is { } lf) candidates.Add(("Last frost", NextOccurrence(today, lf.Month, lf.Day)));
        if (candidates.Count == 0) return null;

        var (label, date) = candidates.MinBy(c => c.Date);
        return (label, date, date.DayNumber - today.DayNumber);
    }

    private static DateOnly NextOccurrence(DateOnly today, int month, int day)
    {
        // Feb 29 in a non-leap year rolls back to Feb 28.
        var candidate = new DateOnly(today.Year, month, Math.Min(day, DateTime.DaysInMonth(today.Year, month)));
        if (candidate < today)
            candidate = new DateOnly(today.Year + 1, month, Math.Min(day, DateTime.DaysInMonth(today.Year + 1, month)));
        return candidate;
    }
}
