namespace History.Tests;

public sealed class HistoryExpiryTests
{
    private static readonly TimeZoneInfo Denver = TimeZoneInfo.FindSystemTimeZoneById("America/Denver");

    [Fact]
    public void Today_LateEveningLocal_IsStillTheLocalDate()
    {
        // 03:00 UTC on the 5th is 20:00 on the 4th in Denver (MST, UTC−7)
        var now = new DateTimeOffset(2026, 11, 5, 3, 0, 0, TimeSpan.Zero);

        Assert.Equal(new DateOnly(2026, 11, 4), HistoryExpiry.Today(now, Denver));
    }

    [Fact]
    public void NextMidnight_IsTheNextLocalMidnightInUtc()
    {
        var now = new DateTimeOffset(2026, 11, 5, 3, 0, 0, TimeSpan.Zero);

        Assert.Equal(new DateTimeOffset(2026, 11, 5, 7, 0, 0, TimeSpan.Zero), HistoryExpiry.NextMidnight(now, Denver));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Not/AZone")]
    public void Zone_MissingOrUnknown_FallsBackToUtc(string? id)
    {
        Assert.Equal(TimeZoneInfo.Utc, HistoryExpiry.Zone(id));
    }
}
