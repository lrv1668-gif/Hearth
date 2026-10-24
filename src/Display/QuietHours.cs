using Display.Records;

namespace Display;

/// <summary>
/// Settings → Frame's quiet hours: a nightly window in which the panel keeps its picture
/// rather than redrawing on the 15-minute or photo interval (each redraw flashes for ~30 s).
/// A mode change or an invalidating edit still renders; see <see cref="FrameRenderService.Due"/>.
/// </summary>
public static class QuietHours
{
    /// <summary>
    /// When the quiet window holding <paramref name="at"/> ends, or null when <paramref name="at"/>
    /// is outside it (or no window is set). A window ending before it starts runs past midnight.
    /// </summary>
    public static DateTimeOffset? EndOf(FrameSettings? saved, DateTimeOffset at, TimeZoneInfo zone)
    {
        if (saved?.QuietStartTime is not { } start || saved.QuietEndTime is not { } end) return null;
        var local = TimeZoneInfo.ConvertTime(at, zone).DateTime;
        var time = TimeOnly.FromDateTime(local);
        var today = local.Date;
        DateTime? endsAt = start < end
            ? time >= start && time < end ? today + end.ToTimeSpan() : null
            : time >= start ? today.AddDays(1) + end.ToTimeSpan()
            : time < end ? today + end.ToTimeSpan()
            : null;
        return endsAt is { } ends ? ModeResolver.ToInstant(ends, zone) : null;
    }
}
