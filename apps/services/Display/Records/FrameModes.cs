using System.Globalization;
using System.Text.Json.Serialization;

namespace Display.Records;

/// <summary>
/// The frame's modes — the ids /frame?mode= takes. Kept in sync with the frontend's
/// constants/frameModes.ts. The order is the cycle <c>POST /display/mode/next</c> steps through.
/// </summary>
public static class FrameModes
{
    public const string Ambient = "ambient";
    public const string Daily = "daily";
    public const string Photo = "photo";
    public const string Countdown = "countdown";
    public const string Night = "night";
    public const string Note = "note";

    public static readonly string[] All = [Ambient, Daily, Photo, Countdown, Night, Note];

    public static bool IsValid(string? mode) => mode is not null && All.Contains(mode);

    /// <summary>The mode after <paramref name="mode"/> in <see cref="All"/>, wrapping round.</summary>
    public static string Next(string mode) => All[(Array.IndexOf(All, mode) + 1) % All.Length];
}

/// <summary>
/// One time-of-day rule: show <paramref name="Mode"/> on <paramref name="Days"/> from
/// <paramref name="Start"/> to <paramref name="End"/> ("HH:mm", household time). An End
/// before Start runs past midnight into the next day; equal times mean all day.
/// </summary>
/// <param name="Days">"mon".."sun" — the days the window starts on.</param>
/// <param name="Rotate">
/// Modes to step on to after <paramref name="Mode"/>, one every <paramref name="RotateMinutes"/>,
/// wrapping round — a TRMNL-style playlist. Each window starts on <paramref name="Mode"/>.
/// </param>
public sealed record ModeRule(
    string Mode, IReadOnlyList<string> Days, string Start, string End,
    IReadOnlyList<string>? Rotate = null, int? RotateMinutes = null)
{
    public static readonly string[] DayIds = ["sun", "mon", "tue", "wed", "thu", "fri", "sat"];
    public const int MaxRotate = 5;
    public const int MinRotateMinutes = 5;
    public const int MaxRotateMinutes = 24 * 60;

    /// <summary>The modes the rule shows, in order: <see cref="Mode"/>, then <see cref="Rotate"/>.</summary>
    [JsonIgnore] public IReadOnlyList<string> Modes => [Mode, .. Rotate ?? []];

    [JsonIgnore] public bool Rotates => Rotate is { Count: > 0 };

    [JsonIgnore] public TimeOnly StartTime => TimeOnly.ParseExact(Start, "HH:mm", CultureInfo.InvariantCulture);
    [JsonIgnore] public TimeOnly EndTime => TimeOnly.ParseExact(End, "HH:mm", CultureInfo.InvariantCulture);

    public bool StartsOn(DayOfWeek day) => Days.Contains(DayIds[(int)day]);

    public string? Validate()
    {
        if (!FrameModes.IsValid(Mode)) return $"mode must be one of {string.Join(", ", FrameModes.All)}";
        if (Days is null || Days.Count == 0) return "a rule needs at least one day";
        if (Days.Any(d => !DayIds.Contains(d))) return "days must be sun, mon, tue, wed, thu, fri or sat";
        if (!IsTime(Start) || !IsTime(End)) return "start and end must be HH:mm";
        if (Rotate is not null && Rotate.Any(m => !FrameModes.IsValid(m)))
            return $"rotate modes must be one of {string.Join(", ", FrameModes.All)}";
        if (Rotate is { Count: > MaxRotate }) return $"a rule rotates through at most {MaxRotate + 1} modes";
        if (Rotates && RotateMinutes is not (>= MinRotateMinutes and <= MaxRotateMinutes))
            return $"rotate_minutes must be {MinRotateMinutes}–{MaxRotateMinutes} when a rule rotates";
        return null;
    }

    private static bool IsTime(string? value) =>
        TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}

/// <summary>The default mode plus the time-of-day rules; the first matching rule wins.</summary>
public sealed record ModeSchedule(string DefaultMode, IReadOnlyList<ModeRule> Rules)
{
    public const int MaxRules = 20;

    public static readonly ModeSchedule Default = new(FrameModes.Ambient, []);

    public string? Validate()
    {
        if (!FrameModes.IsValid(DefaultMode))
            return $"default_mode must be one of {string.Join(", ", FrameModes.All)}";
        if (Rules is null) return "rules required";
        if (Rules.Count > MaxRules) return $"at most {MaxRules} rules";
        return Rules.Select(r => r is null ? "rules must not be null" : r.Validate()).FirstOrDefault(e => e is not null);
    }
}

/// <summary>
/// A manual choice that beats the schedule until <paramref name="Until"/> (null: until
/// it's cleared). <paramref name="PhotoId"/> pins one upload in photo mode — the
/// Settings → Photos "Show on frame" switch.
/// </summary>
public sealed record ModeOverride(string Mode, DateTimeOffset? Until = null, string? PhotoId = null)
{
    public string? Validate()
    {
        if (!FrameModes.IsValid(Mode)) return $"mode must be one of {string.Join(", ", FrameModes.All)}";
        if (PhotoId is not null && (Mode != FrameModes.Photo || string.IsNullOrWhiteSpace(PhotoId) || PhotoId.Length > 100))
            return "photo_id only goes with photo mode";
        return null;
    }
}

/// <summary>
/// What the frame shows now and why. <paramref name="Until"/> is when that may next
/// change — the override's expiry or the next schedule boundary; null if nothing is due.
/// </summary>
public sealed record ResolvedMode(string Mode, string Reason, DateTimeOffset? Until, string? PhotoId = null);
