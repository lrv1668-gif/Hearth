using Display.Records;

namespace Display;

/// <summary>
/// Picks the frame's mode: a live override, else the first schedule rule whose window
/// holds the household's current time, else the schedule's default. A rotating rule
/// steps through its modes every <see cref="ModeRule.RotateMinutes"/> from its window's
/// start — worked out from the clock, so a restart doesn't lose its place — and holds
/// its step through <paramref name="quiet"/>'s quiet hours, moving on once they end.
/// </summary>
public static class ModeResolver
{
    public static ResolvedMode Resolve(
        ModeSchedule schedule, ModeOverride? manual, DateTimeOffset now, TimeZoneInfo zone, FrameSettings? quiet = null)
    {
        if (manual is not null && (manual.Until is null || manual.Until > now))
            return new ResolvedMode(manual.Mode, "override", manual.Until, manual.PhotoId);

        var local = TimeZoneInfo.ConvertTime(now, zone).DateTime;
        var until = NextBoundary(schedule, now, zone);
        if (schedule.Rules.FirstOrDefault(rule => Holds(rule, local)) is not { } match)
            return new ResolvedMode(schedule.DefaultMode, "default", until);
        if (!match.Rotates) return new ResolvedMode(match.Mode, "schedule", until);

        var (mode, step) = Rotation(match, local, now, zone, quiet);
        return new ResolvedMode(mode, "schedule", until is null || step < until ? step : until);
    }

    /// <summary>
    /// A rotating rule's mode at <paramref name="now"/> and when it next steps. Inside the
    /// quiet hours it keeps the step it was on when they began (or the window's first, if
    /// the window began during them) until they end.
    /// </summary>
    internal static (string Mode, DateTimeOffset NextStep) Rotation(
        ModeRule rule, DateTime local, DateTimeOffset now, TimeZoneInfo zone, FrameSettings? quiet)
    {
        var start = ToInstant(WindowStart(rule, local), zone);
        var interval = TimeSpan.FromMinutes(rule.RotateMinutes!.Value);
        var held = QuietHours.Holding(quiet, now, zone);
        var at = held is { } q ? (q.Start > start ? q.Start : start) : now;
        var step = (long)((at - start) / interval);
        var mode = rule.Modes[(int)(step % rule.Modes.Count)];
        return (mode, held?.End ?? start + interval * (step + 1));
    }

    /// <summary>When the window of <paramref name="rule"/> holding <paramref name="local"/> began; all-day rules begin at midnight.</summary>
    internal static DateTime WindowStart(ModeRule rule, DateTime local)
    {
        var (start, end) = (rule.StartTime, rule.EndTime);
        if (start == end) return local.Date;
        if (start < end || (rule.StartsOn(local.DayOfWeek) && TimeOnly.FromDateTime(local) >= start))
            return local.Date + start.ToTimeSpan();
        return local.Date.AddDays(-1) + start.ToTimeSpan();
    }

    /// <summary>Whether <paramref name="local"/> falls in the rule's window starting today or (past midnight) yesterday.</summary>
    public static bool Holds(ModeRule rule, DateTime local)
    {
        var time = TimeOnly.FromDateTime(local);
        var (start, end) = (rule.StartTime, rule.EndTime);
        if (start == end) return rule.StartsOn(local.DayOfWeek);
        if (start < end) return rule.StartsOn(local.DayOfWeek) && time >= start && time < end;
        return (rule.StartsOn(local.DayOfWeek) && time >= start)
            || (rule.StartsOn(local.AddDays(-1).DayOfWeek) && time < end);
    }

    /// <summary>The next rule start or end after <paramref name="now"/> within a week, or null with no rules.</summary>
    public static DateTimeOffset? NextBoundary(ModeSchedule schedule, DateTimeOffset now, TimeZoneInfo zone)
    {
        var today = TimeZoneInfo.ConvertTime(now, zone).Date;
        DateTimeOffset? next = null;
        for (var offset = -1; offset <= 7; offset++)
        {
            var day = today.AddDays(offset);
            foreach (var rule in schedule.Rules.Where(r => r.StartsOn(day.DayOfWeek)))
            {
                var start = day + rule.StartTime.ToTimeSpan();
                var end = rule.EndTime > rule.StartTime ? day + rule.EndTime.ToTimeSpan()
                    : day.AddDays(1) + rule.EndTime.ToTimeSpan();
                foreach (var local in new[] { start, end })
                {
                    var at = ToInstant(local, zone);
                    if (at > now && (next is null || at < next)) next = at;
                }
            }
        }
        return next;
    }

    /// <summary>A household wall-clock time as an instant; a time skipped by DST moves past the gap.</summary>
    internal static DateTimeOffset ToInstant(DateTime local, TimeZoneInfo zone)
    {
        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        while (zone.IsInvalidTime(unspecified)) unspecified = unspecified.AddMinutes(30);
        return new DateTimeOffset(unspecified, zone.GetUtcOffset(unspecified));
    }
}
