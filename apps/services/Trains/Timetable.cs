using System.Globalization;
using Trains.Records;

namespace Trains;

/// <summary>
/// Builds the frame's Scheduled Departures: a printed-timetable block that holds still
/// for the clock hour. The frame re-renders every 15 minutes and each change is a ~30 s
/// panel redraw, so live predictions stay on the dashboard; this shows timetabled times
/// only, picked once an hour.
/// </summary>
public static class Timetable
{
    public const int Hours = 2;

    // Transitland documents no ceiling for limit; 100 is the common page cap across its
    // API, and covers a busy stop's two hours once the household's line filter applies.
    public const int FetchLimit = 100;

    /// <summary>The top of the clock hour <paramref name="now"/> is in, in UTC.</summary>
    public static DateTimeOffset HourOf(DateTimeOffset now)
    {
        var utc = now.ToUniversalTime();
        return new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, TimeSpan.Zero);
    }

    /// <summary>Seconds to ask Transitland for, from <paramref name="now"/> to the window's end.</summary>
    public static int NextSeconds(DateTimeOffset now) =>
        (int)Math.Ceiling((HourOf(now).AddHours(Hours) - now).TotalSeconds);

    /// <summary>
    /// The departures inside [the hour, the hour + <see cref="Hours"/>), grouped by line and
    /// direction (the dashboard's line-filter key), lines in order of their first departure.
    /// A departure Transitland has no scheduled instant for is left out.
    /// </summary>
    public static StopTimetable Build(StopDepartures stop, DateTimeOffset now)
    {
        var start = HourOf(now);
        var end = start.AddHours(Hours);

        var lines = stop.Departures
            .Select(d => (Departure: d, At: ParseUtc(d.ScheduledUtc)))
            .Where(d => d.At is { } at && at >= start && at < end)
            .GroupBy(d => (d.Departure.RouteShortName, Headsign: d.Departure.Headsign ?? "", d.Departure.Mode))
            .Select(g => new TimetableLine(
                g.Key.RouteShortName,
                g.Key.Headsign == "" ? null : g.Key.Headsign,
                g.Key.Mode,
                g.Select(d => d.At!.Value).Distinct().Order().Select(Iso).ToList()))
            .OrderBy(l => l.Departures[0], StringComparer.Ordinal)
            .ThenBy(l => l.RouteShortName, StringComparer.Ordinal)
            .ToList();

        return new StopTimetable(stop.StopKey, stop.StopName, Iso(start), Iso(end), lines);
    }

    private static DateTimeOffset? ParseUtc(string? value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var at)
            ? at.ToUniversalTime()
            : null;

    private static string Iso(DateTimeOffset at) =>
        at.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
}
