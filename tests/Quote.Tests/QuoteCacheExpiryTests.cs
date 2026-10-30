namespace Quote.Tests;

public sealed class QuoteCacheExpiryTests
{
    [Theory]
    [InlineData(13, 30, 0, 0)]      // mid-day
    [InlineData(0, 0, 0, 0)]        // exactly midnight: the next one, not now
    [InlineData(23, 59, 59, 999)]   // last moment of the day
    public void NextMidnightUtc_AnyTimeOnTheDay_ReturnsTheFollowingMidnight(int hour, int minute, int second, int ms)
    {
        var now = new DateTime(2026, 6, 15, hour, minute, second, ms, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2026, 6, 16, 0, 0, 0, DateTimeKind.Utc), QuoteCacheExpiry.NextMidnightUtc(now));
    }
}
