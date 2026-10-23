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
