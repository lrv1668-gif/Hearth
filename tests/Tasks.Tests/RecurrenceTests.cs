using Tasks;

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
    public void ComputeNextDue_MonthWithAnchorDay_ReturnsToItAfterAShortMonth()
    {
        // A series on the 31st: Feb clamps to the 28th, then March is back on the 31st
        var feb = TaskStore.ComputeNextDue(new DateTime(2026, 1, 31), "month", 1, null, anchorDay: 31);
        var mar = TaskStore.ComputeNextDue(feb, "month", 1, null, anchorDay: 31);
        var apr = TaskStore.ComputeNextDue(mar, "month", 1, null, anchorDay: 31);

        Assert.Equal(new DateTime(2026, 2, 28), feb);
        Assert.Equal(new DateTime(2026, 3, 31), mar);
        Assert.Equal(new DateTime(2026, 4, 30), apr);
    }

    [Fact]
    public void ComputeNextDue_YearlyOnLeapDay_ReturnsToItInTheNextLeapYear()
    {
        var current = new DateTime(2028, 2, 29);
        for (var year = 0; year < 4; year++)
            current = TaskStore.ComputeNextDue(current, "month", 12, null, anchorDay: 29);

        Assert.Equal(new DateTime(2032, 2, 29), current);
    }

    [Fact]
    public void ComputeNextDue_EveryOtherWeekWithDays_SkipsTheWeekBetween()
    {
        // 2026-06-01 is a Monday. Mon and Wed every other week: Wed the 3rd, then Mon the 15th
        var wed = TaskStore.ComputeNextDue(new DateTime(2026, 6, 1), "week", 2, "Mon,Wed");
        var mon = TaskStore.ComputeNextDue(wed, "week", 2, "Mon,Wed");
        var nextWed = TaskStore.ComputeNextDue(mon, "week", 2, "Mon,Wed");

        Assert.Equal(new DateTime(2026, 6, 3), wed);
        Assert.Equal(new DateTime(2026, 6, 15), mon);
        Assert.Equal(new DateTime(2026, 6, 17), nextWed);
    }

    [Fact]
    public void ComputeNextDue_EveryOtherWeekOnSunday_StaysInTheMondayToSundayWeek()
    {
        // Sat the 6th and Sun the 7th share a week; the next Saturday is two weeks on
        var sun = TaskStore.ComputeNextDue(new DateTime(2026, 6, 6), "week", 2, "Sat,Sun");
        var sat = TaskStore.ComputeNextDue(sun, "week", 2, "Sat,Sun");

        Assert.Equal(new DateTime(2026, 6, 7), sun);
        Assert.Equal(new DateTime(2026, 6, 20), sat);
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
        // 2026-06-01 is a Monday
        var result = TaskStore.ComputeNextDue(new DateTime(2026, 6, 1), "week", 1, "Mon,Wed,Fri");

        Assert.Equal(new DateTime(2026, 6, 3), result);
    }

    [Fact]
    public void NextMatchingWeekday_AllDaysSelected_ReturnsNextDay()
    {
        var from = new DateTime(2026, 6, 1);
        var result = TaskStore.NextMatchingWeekday(from, "Mon,Tue,Wed,Thu,Fri,Sat,Sun");

        Assert.Equal(from.AddDays(1), result);
    }

    [Fact]
    public void NextMatchingWeekday_LaterThisWeek_ReturnsThatDay()
    {
        Assert.Equal(new DateTime(2026, 6, 7), TaskStore.NextMatchingWeekday(new DateTime(2026, 6, 1), "Sun"));
    }

    [Fact]
    public void NextMatchingWeekday_SameWeekday_ReturnsAWeekLater()
    {
        Assert.Equal(new DateTime(2026, 6, 8), TaskStore.NextMatchingWeekday(new DateTime(2026, 6, 1), "Mon"));
    }

    [Fact]
    public void NextMatchingWeekday_UnknownToken_DefaultsToMonday()
    {
        Assert.Equal(new DateTime(2026, 6, 8), TaskStore.NextMatchingWeekday(new DateTime(2026, 6, 1), "Xyz"));
    }
}
