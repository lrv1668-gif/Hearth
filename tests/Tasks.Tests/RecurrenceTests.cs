using Tasks;
using Xunit;

namespace Tasks.Tests;

public sealed class RecurrenceTests
{
    [Theory]
    [InlineData("2026-06-01", "day", 1, "2026-06-02")]
    [InlineData("2026-06-01", "day", 3, "2026-06-04")]
    [InlineData("2026-06-01", "week", 1, "2026-06-08")]
    [InlineData("2026-06-01", "week", 2, "2026-06-15")]
    [InlineData("2026-06-15", "month", 1, "2026-07-15")]
    [InlineData("2026-06-15", "month", 2, "2026-08-15")]
    public void ComputeNextDue_SimpleUnits_AddsExpectedInterval(
        string current, string unit, int interval, string expected)
    {
        var result = TaskStore.ComputeNextDue(DateTime.Parse(current), unit, interval, null);
        Assert.Equal(DateTime.Parse(expected), result);
    }

    [Fact]
    public void ComputeNextDue_MonthOnLongDay_RollsBackToShorterMonth()
    {
        // Jan 31 + 1 month has no Feb 31; DateTime.AddMonths clamps to Feb 28.
        var result = TaskStore.ComputeNextDue(new DateTime(2026, 1, 31), "month", 1, null);
        Assert.Equal(new DateTime(2026, 2, 28), result);
    }

    [Fact]
    public void ComputeNextDue_UnknownUnit_FallsBackToDayInterval()
    {
        var result = TaskStore.ComputeNextDue(new DateTime(2026, 6, 1), "fortnight", 5, null);
        Assert.Equal(new DateTime(2026, 6, 6), result);
    }

    [Fact]
    public void ComputeNextDue_WeekWithDays_ReturnsNextMatchingWeekday()
    {
        var from = new DateTime(2026, 6, 1);
        var result = TaskStore.ComputeNextDue(from, "week", 1, "Mon,Wed,Fri");

        Assert.Contains(result.DayOfWeek, new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday });
        Assert.True(result > from);
    }

    [Fact]
    public void NextMatchingWeekday_AllDaysSelected_ReturnsNextDay()
    {
        var from = new DateTime(2026, 6, 1);
        var result = TaskStore.NextMatchingWeekday(from, "Mon,Tue,Wed,Thu,Fri,Sat,Sun");

        Assert.Equal(from.AddDays(1), result);
    }

    [Fact]
    public void NextMatchingWeekday_ReturnsDateWithinFollowingWeek()
    {
        var from = new DateTime(2026, 6, 1);
        var result = TaskStore.NextMatchingWeekday(from, "Sun");

        Assert.Equal(DayOfWeek.Sunday, result.DayOfWeek);
        Assert.True(result > from && result <= from.AddDays(7));
    }

    [Fact]
    public void NextMatchingWeekday_UnknownToken_DefaultsToMonday()
    {
        var from = new DateTime(2026, 6, 1);
        var result = TaskStore.NextMatchingWeekday(from, "Xyz");

        Assert.Equal(DayOfWeek.Monday, result.DayOfWeek);
    }
}
