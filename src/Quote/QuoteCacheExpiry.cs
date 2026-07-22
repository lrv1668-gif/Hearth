namespace Quote;

public static class QuoteCacheExpiry
{
    public static DateTime NextMidnightUtc(DateTime utcNow) =>
        DateOnly.FromDateTime(utcNow).AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
}
