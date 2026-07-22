using Xunit;

namespace Quote.Tests;

public sealed class QuoteCacheExpiryTests
{
    [Fact]
    public void NextMidnightUtc_MidDay_ReturnsFollowingMidnight()
    {
        var now = new DateTime(2026, 6, 15, 13, 30, 0, DateTimeKind.Utc);

        var expiry = QuoteCacheExpiry.NextMidnightUtc(now);

        Assert.Equal(new DateTime(2026, 6, 16, 0, 0, 0, DateTimeKind.Utc), expiry);
    }

    [Fact]
    public void NextMidnightUtc_ExactlyMidnight_ReturnsNextDayMidnight()
    {
        var now = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);

        var expiry = QuoteCacheExpiry.NextMidnightUtc(now);

        Assert.Equal(new DateTime(2026, 6, 16, 0, 0, 0, DateTimeKind.Utc), expiry);
    }

    [Fact]
    public void NextMidnightUtc_LastMomentOfDay_ReturnsNextDayMidnight()
    {
        var now = new DateTime(2026, 6, 15, 23, 59, 59, 999, DateTimeKind.Utc);

        var expiry = QuoteCacheExpiry.NextMidnightUtc(now);

        Assert.Equal(new DateTime(2026, 6, 16, 0, 0, 0, DateTimeKind.Utc), expiry);
    }
}
