using Display;
using Display.Records;
using Xunit;

namespace Display.Tests;

public sealed class ModeResolverTests
{
    private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;

    // Monday 19 October 2026
    private static DateTimeOffset Mon(int hour, int minute = 0) => new(2026, 10, 19, hour, minute, 0, TimeSpan.Zero);

    private static readonly ModeRule Mornings = new(FrameModes.Daily, ["mon", "tue", "wed", "thu", "fri"], "07:00", "09:00");
    private static readonly ModeRule Nights = new(FrameModes.Night, ["mon", "tue", "wed", "thu", "fri", "sat", "sun"], "23:00", "06:00");
    private static readonly ModeSchedule Schedule = new(FrameModes.Ambient, [Mornings, Nights]);

    [Fact]
    public void Resolve_NoRulesNoOverride_ReturnsDefaultWithNoUntil()
    {
        var resolved = ModeResolver.Resolve(ModeSchedule.Default, null, Mon(12), Utc);

        Assert.Equal(new ResolvedMode(FrameModes.Ambient, "default", null), resolved);
    }

    [Fact]
    public void Resolve_InsideRule_ReturnsRuleUntilItsEnd()
    {
        var resolved = ModeResolver.Resolve(Schedule, null, Mon(8), Utc);

        Assert.Equal(new ResolvedMode(FrameModes.Daily, "schedule", Mon(9)), resolved);
    }

    [Fact]
    public void Resolve_OutsideRules_ReturnsDefaultUntilNextRuleStarts()
    {
        var resolved = ModeResolver.Resolve(Schedule, null, Mon(12), Utc);

        Assert.Equal(new ResolvedMode(FrameModes.Ambient, "default", Mon(23)), resolved);
    }

    [Fact]
    public void Resolve_RuleEndingAtItsEnd_IsExclusive()
    {
        Assert.Equal(FrameModes.Ambient, ModeResolver.Resolve(Schedule, null, Mon(9), Utc).Mode);
    }

    [Fact]
    public void Resolve_AfterMidnightInAnOvernightRule_StillMatches()
    {
        // Tuesday 02:00 — inside Monday's 23:00–06:00 window
        var resolved = ModeResolver.Resolve(Schedule, null, Mon(2).AddDays(1), Utc);

        Assert.Equal(new ResolvedMode(FrameModes.Night, "schedule", Mon(6).AddDays(1)), resolved);
    }

    [Fact]
    public void Resolve_OvernightRule_UsesTheDayItStarted()
    {
        var fridayOnly = new ModeSchedule(FrameModes.Ambient, [Nights with { Days = ["fri"] }]);

        // Saturday 02:00 follows Friday night; Friday 02:00 follows a Thursday it doesn't run on
        Assert.Equal(FrameModes.Night, ModeResolver.Resolve(fridayOnly, null, Mon(2).AddDays(5), Utc).Mode);
        Assert.Equal(FrameModes.Ambient, ModeResolver.Resolve(fridayOnly, null, Mon(2).AddDays(4), Utc).Mode);
    }

    [Fact]
    public void Resolve_RuleOnOtherDays_DoesNotMatch()
    {
        var weekends = new ModeSchedule(FrameModes.Ambient, [Mornings with { Days = ["sat", "sun"] }]);

        Assert.Equal(FrameModes.Ambient, ModeResolver.Resolve(weekends, null, Mon(8), Utc).Mode);
    }

    [Fact]
    public void Resolve_EqualStartAndEnd_MatchesAllDay()
    {
        var allMonday = new ModeSchedule(FrameModes.Ambient, [new ModeRule(FrameModes.Photo, ["mon"], "00:00", "00:00")]);

        Assert.Equal(FrameModes.Photo, ModeResolver.Resolve(allMonday, null, Mon(15), Utc).Mode);
    }

    [Fact]
    public void Resolve_OverlappingRules_FirstWins()
    {
        var both = new ModeSchedule(FrameModes.Ambient, [Mornings, Mornings with { Mode = FrameModes.Countdown }]);

        Assert.Equal(FrameModes.Daily, ModeResolver.Resolve(both, null, Mon(8), Utc).Mode);
    }

    [Fact]
    public void Resolve_LiveOverride_BeatsSchedule()
    {
        var manual = new ModeOverride(FrameModes.Photo, Mon(10));

        var resolved = ModeResolver.Resolve(Schedule, manual, Mon(8), Utc);

        Assert.Equal(new ResolvedMode(FrameModes.Photo, "override", Mon(10)), resolved);
    }

    [Fact]
    public void Resolve_PinnedPhoto_HoldsWithNoExpiry()
    {
        var pin = new ModeOverride(FrameModes.Photo, null, "abc123");

        var resolved = ModeResolver.Resolve(Schedule, pin, Mon(8).AddDays(30), Utc);

        Assert.Equal(new ResolvedMode(FrameModes.Photo, "override", null, "abc123"), resolved);
    }

    [Fact]
    public void Resolve_ExpiredOverride_IsIgnored()
    {
        var manual = new ModeOverride(FrameModes.Photo, Mon(7, 30));

        Assert.Equal("schedule", ModeResolver.Resolve(Schedule, manual, Mon(8), Utc).Reason);
    }

    [Fact]
    public void Resolve_HouseholdZone_MatchesLocalTime()
    {
        var chicago = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");

        // 13:00 UTC is 08:00 in Chicago (CDT, UTC−5) — inside the morning rule, ending 14:00 UTC
        var resolved = ModeResolver.Resolve(Schedule, null, Mon(13), chicago);

        Assert.Equal(new ResolvedMode(FrameModes.Daily, "schedule", Mon(14)), resolved);
    }

    private static readonly ModeRule RotatingMornings =
        Mornings with { Rotate = [FrameModes.Photo, FrameModes.Countdown], RotateMinutes = 30 };

    private static FrameSettings Quiet(string start, string end) =>
        new("almanac", "inter", "landscape", [], [], 50, QuietStart: start, QuietEnd: end);

    [Theory]
    [InlineData(7, 0, "daily", 7, 30)]
    [InlineData(7, 45, "photo", 8, 0)]
    [InlineData(8, 10, "countdown", 8, 30)]
    [InlineData(8, 40, "daily", 9, 0)] // wraps round
    public void Resolve_RotatingRule_StepsEveryIntervalFromTheWindowStart(
        int hour, int minute, string mode, int untilHour, int untilMinute)
    {
        var schedule = new ModeSchedule(FrameModes.Ambient, [RotatingMornings]);

        var resolved = ModeResolver.Resolve(schedule, null, Mon(hour, minute), Utc);

        Assert.Equal(new ResolvedMode(mode, "schedule", Mon(untilHour, untilMinute)), resolved);
    }

    [Fact]
    public void Resolve_RotatingRule_LastStepEndsWithTheWindow()
    {
        // Steps at 07:00, 07:45, 08:30; the next (09:15) is past the window's 09:00 end
        var schedule = new ModeSchedule(FrameModes.Ambient, [RotatingMornings with { RotateMinutes = 45 }]);

        Assert.Equal(new ResolvedMode(FrameModes.Countdown, "schedule", Mon(9)), ModeResolver.Resolve(schedule, null, Mon(8, 40), Utc));
    }

    [Fact]
    public void Resolve_RotatingOvernightRule_CountsFromTheEveningStart()
    {
        var schedule = new ModeSchedule(FrameModes.Ambient, [Nights with { Rotate = [FrameModes.Ambient], RotateMinutes = 60 }]);
        var tuesday = Mon(0).AddDays(1);

        Assert.Equal(FrameModes.Ambient, ModeResolver.Resolve(schedule, null, tuesday.AddMinutes(30), Utc).Mode);
        Assert.Equal(FrameModes.Night, ModeResolver.Resolve(schedule, null, tuesday.AddMinutes(90), Utc).Mode);
    }

    [Fact]
    public void Resolve_RotatingRuleInQuietHours_HoldsTheStepTheyBeganOnUntilTheyEnd()
    {
        var schedule = new ModeSchedule(FrameModes.Ambient, [Nights with { Rotate = [FrameModes.Ambient], RotateMinutes = 60 }]);
        var tuesday = Mon(0).AddDays(1);
        var quiet = Quiet("00:00", "05:00");

        // 00:00 is the second hourly step (Ambient); it holds to 05:00, then the clock takes over again
        Assert.Equal(new ResolvedMode(FrameModes.Ambient, "schedule", tuesday.AddHours(5)),
            ModeResolver.Resolve(schedule, null, tuesday.AddHours(2.5), Utc, quiet));
        Assert.Equal(new ResolvedMode(FrameModes.Night, "schedule", tuesday.AddHours(6)),
            ModeResolver.Resolve(schedule, null, tuesday.AddHours(5.5), Utc, quiet));
    }

    [Fact]
    public void Resolve_RotatingRuleStartingInQuietHours_HoldsItsFirstMode()
    {
        var schedule = new ModeSchedule(FrameModes.Ambient, [Nights with { Rotate = [FrameModes.Ambient], RotateMinutes = 60 }]);
        var tuesday = Mon(0).AddDays(1);

        Assert.Equal(new ResolvedMode(FrameModes.Night, "schedule", tuesday.AddHours(5)),
            ModeResolver.Resolve(schedule, null, tuesday.AddHours(2), Utc, Quiet("22:00", "05:00")));
    }

    [Fact]
    public void Resolve_QuietHours_DoNotChangeAPlainRule()
    {
        Assert.Equal(new ResolvedMode(FrameModes.Daily, "schedule", Mon(9)),
            ModeResolver.Resolve(Schedule, null, Mon(8), Utc, Quiet("07:00", "10:00")));
    }

    [Fact]
    public void RuleValidate_BadRotation_ReturnsError()
    {
        Assert.NotNull((RotatingMornings with { Rotate = ["poster"] }).Validate());
        Assert.NotNull((RotatingMornings with { RotateMinutes = null }).Validate());
        Assert.NotNull((RotatingMornings with { RotateMinutes = 1 }).Validate());
        Assert.NotNull((RotatingMornings with { Rotate = [.. FrameModes.All] }).Validate());
        Assert.Null(RotatingMornings.Validate());
        Assert.Null((Mornings with { Rotate = [] }).Validate());
    }

    [Theory]
    [InlineData("ambient", "daily")]
    [InlineData("night", "note")]
    [InlineData("note", "ambient")]
    public void Next_StepsThroughModesAndWraps(string from, string to)
    {
        Assert.Equal(to, FrameModes.Next(from));
    }

    [Fact]
    public void ScheduleValidate_BadRule_ReturnsError()
    {
        Assert.NotNull(new ModeSchedule(FrameModes.Ambient, [Mornings with { Start = "7am" }]).Validate());
        Assert.NotNull(new ModeSchedule(FrameModes.Ambient, [Mornings with { Days = ["monday"] }]).Validate());
        Assert.NotNull(new ModeSchedule(FrameModes.Ambient, [Mornings with { Mode = "poster" }]).Validate());
        Assert.NotNull(new ModeSchedule("poster", []).Validate());
        Assert.Null(Schedule.Validate());
    }

    [Fact]
    public void OverrideValidate_PhotoIdOutsidePhotoMode_ReturnsError()
    {
        Assert.NotNull(new ModeOverride(FrameModes.Daily, null, "abc").Validate());
        Assert.Null(new ModeOverride(FrameModes.Photo, null, "abc").Validate());
    }
}
