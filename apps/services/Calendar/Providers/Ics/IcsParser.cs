using System.Globalization;
using Calendar.Records;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using ICalendar = Ical.Net.Calendar;

namespace Calendar.Providers.Ics;

/// <summary>
/// Turns iCalendar text into <see cref="CalendarItem"/>s. Ical.Net does the parsing and the
/// recurrence expansion (RRULE, EXDATE, RECURRENCE-ID overrides, TZID); this maps the result.
/// </summary>
public static class IcsParser
{
    // A rule like FREQ=MINUTELY would otherwise fill the window with thousands of rows
    internal const int MaxItems = 2000;

    // How far before the window to start expanding, so an event already under way is included
    private static readonly TimeSpan LookBehind = TimeSpan.FromDays(31);

    /// <summary>Throws <see cref="IcsFeedException"/> when the text isn't an iCalendar file.</summary>
    public static ICalendar Load(string ics)
    {
        ICalendar? calendar;
        try
        {
            calendar = ICalendar.Load(ics);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            calendar = null;
        }
        return calendar ?? throw new IcsFeedException("That link didn't return a calendar (an .ics file).");
    }

    /// <summary>The calendar's own name (<c>X-WR-CALNAME</c>, which Apple and Google both write), if it has one.</summary>
    public static string? NameOf(ICalendar calendar) =>
        calendar.Properties["X-WR-CALNAME"]?.Value?.ToString()?.Trim() is { Length: > 0 } name ? name : null;

    /// <summary>
    /// The events between <paramref name="from"/> and <paramref name="to"/>, recurring ones
    /// expanded to one item per occurrence. Times are written in <paramref name="homeZone"/>
    /// (the frontend shows the wall-clock time in the string), which is also where floating
    /// times — ones with no zone — are taken to be. Without a home zone, each time keeps its own.
    /// </summary>
    public static List<CalendarItem> Expand(
        ICalendar calendar, IcsFeed feed, DateTimeOffset from, DateTimeOffset to, TimeZoneInfo? homeZone)
    {
        var fromUtc = from.UtcDateTime;
        var toUtc   = to.UtcDateTime;
        var items   = new List<CalendarItem>();

        var occurrences = calendar
            .GetOccurrences<CalendarEvent>(new CalDateTime(fromUtc - LookBehind))
            .TakeWhile(o => o.Period.StartTime.AsUtc < toUtc);

        foreach (var occurrence in occurrences)
        {
            if (occurrence.Source is not CalendarEvent e) continue;
            if (string.Equals(e.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase)) continue;

            var start = occurrence.Period.StartTime;
            var end   = occurrence.Period.EffectiveEndTime ?? start;
            var isAllDay = !start.HasTime;

            // Skip what ended before the window: an all-day end is an exclusive date, a timed end an instant
            var ended = isAllDay
                ? (end.Value.Date > start.Value.Date ? end.Value.Date : start.Value.Date.AddDays(1)) <= fromUtc.Date
                : end.AsUtc <= fromUtc;
            if (ended) continue;

            items.Add(new CalendarItem(
                Kind:         "event",
                Id:           $"{feed.Id}:{e.Uid}:{start.AsUtc:yyyyMMddTHHmmss}",
                Title:        string.IsNullOrWhiteSpace(e.Summary) ? "(No title)" : e.Summary.Trim(),
                Description:  string.IsNullOrWhiteSpace(e.Description) ? null : e.Description,
                Location:     string.IsNullOrWhiteSpace(e.Location) ? null : e.Location,
                Start:        isAllDay ? DateKey(start) : Stamp(start, homeZone),
                End:          isAllDay ? DateKey(end) : Stamp(end, homeZone),
                IsAllDay:     isAllDay,
                CalendarName: feed.Name,
                Provider:     IcsCalendarProvider.Key,
                IsCompleted:  null,
                TaskListId:   null,
                HtmlLink:     e.Url is { IsAbsoluteUri: true } link && (link.Scheme == Uri.UriSchemeHttp || link.Scheme == Uri.UriSchemeHttps)
                                  ? link.AbsoluteUri
                                  : null));

            if (items.Count >= MaxItems) break;
        }

        return items;
    }

    private static string DateKey(CalDateTime date) =>
        date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Stamp(CalDateTime at, TimeZoneInfo? homeZone)
    {
        var wallClock = DateTime.SpecifyKind(at.Value, DateTimeKind.Unspecified);

        DateTimeOffset stamp;
        if (at.IsFloating)
            stamp = new DateTimeOffset(wallClock, homeZone?.GetUtcOffset(wallClock) ?? TimeSpan.Zero);
        else if (homeZone is not null)
            stamp = TimeZoneInfo.ConvertTime(new DateTimeOffset(at.AsUtc, TimeSpan.Zero), homeZone);
        else
            stamp = new DateTimeOffset(wallClock, wallClock - DateTime.SpecifyKind(at.AsUtc, DateTimeKind.Unspecified));

        return stamp.ToString("o", CultureInfo.InvariantCulture);
    }
}
