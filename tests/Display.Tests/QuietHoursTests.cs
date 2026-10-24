using Display.Records;
using Xunit;

namespace Display.Tests;

public sealed class QuietHoursTests
{
    // Monday 19 October 2026, 00:00 UTC
    private static readonly DateTimeOffset Midnight = new(2026, 10, 19, 0, 0, 0, TimeSpan.Zero);

    private static readonly FrameSettings Overnight =
        new("almanac", "inter", "landscape", [], [], 50, QuietStart: "23:00", QuietEnd: "06:00");

    [Theory]
    [InlineData(23, 30, 30)] // 23:30 → 06:00 tomorrow
    [InlineData(2, 0, 6)]    // 02:00 → 06:00 today
    public void EndOf_InsideAnOvernightWindow_IsItsEnd(int hour, int minute, int endHoursFromMidnight)
    {
        var at = Midnight.AddHours(hour).AddMinutes(minute);
        Assert.Equal(Midnight.AddHours(endHoursFromMidnight), QuietHours.EndOf(Overnight, at, TimeZoneInfo.Utc));
    }

    [Theory]
    [InlineData(6, 0)]
    [InlineData(12, 0)]
    [InlineData(22, 59)]
    public void EndOf_OutsideTheWindow_IsNull(int hour, int minute)
    {
        Assert.Null(QuietHours.EndOf(Overnight, Midnight.AddHours(hour).AddMinutes(minute), TimeZoneInfo.Utc));
    }

    [Fact]
    public void EndOf_SameDayWindow_EndsToday()
    {
        var saved = Overnight with { QuietStart = "01:00", QuietEnd = "05:00" };
        Assert.Equal(Midnight.AddHours(5), QuietHours.EndOf(saved, Midnight.AddHours(3), TimeZoneInfo.Utc));
        Assert.Null(QuietHours.EndOf(saved, Midnight.AddHours(5), TimeZoneInfo.Utc));
    }

    [Fact]
    public void EndOf_NoWindowOrNothingSaved_IsNull()
    {
        Assert.Null(QuietHours.EndOf(Overnight with { QuietStart = null, QuietEnd = null }, Midnight, TimeZoneInfo.Utc));
        Assert.Null(QuietHours.EndOf(null, Midnight, TimeZoneInfo.Utc));
    }

    [Fact]
    public void EndOf_HouseholdZone_UsesItsWallClock()
    {
        // 04:00 UTC is midnight in New York (EDT), inside the window, which ends at 06:00 there
        var zone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var end = QuietHours.EndOf(Overnight, Midnight.AddHours(4), zone);
        Assert.Equal(new DateTimeOffset(2026, 10, 19, 6, 0, 0, TimeSpan.FromHours(-4)), end);
    }
}
