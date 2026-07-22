using System.Text.Json;
using Calendar.Providers.Google;
using Calendar.Records;
using Calendar.Tests.Helpers;
using Data.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Calendar.Tests;

public sealed class GoogleCalendarProviderTests
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private static (GoogleCalendarProvider Provider, CalendarStore Store) MakeProvider(TempDatabase db)
    {
        var store = new CalendarStore(db.Db, TestDataProtection.Provider);
        store.Migrate();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GOOGLE_CLIENT_ID"] = "test-client-id",
                ["GOOGLE_CLIENT_SECRET"] = "test-client-secret",
                ["GOOGLE_REDIRECT_URI"] = "https://example.com/calendar/google/callback",
            })
            .Build();

        var authService = new GoogleAuthService(store, config, TimeProvider.System);
        var provider = new GoogleCalendarProvider(store, authService, NullLogger<GoogleCalendarProvider>.Instance);
        return (provider, store);
    }

    private static void SeedCacheAt(TempDatabase db, string provider, string json, DateTimeOffset cachedAt) =>
        db.Db.NonQuery("""
            INSERT OR REPLACE INTO calendar_items_cache (provider, items_json, cached_at)
            VALUES ($provider, $json, $cached_at)
            """, cmd =>
        {
            cmd.AddParam("$provider", provider);
            cmd.AddParam("$json", json);
            cmd.AddParam("$cached_at", cachedAt.ToString("o"));
        });

    private static List<CalendarItem> SampleItems() =>
    [
        new CalendarItem(
            Kind: "event", Id: "evt-1", Title: "Standup", Description: null, Location: null,
            Start: "2026-07-20T09:00:00+00:00", End: "2026-07-20T09:30:00+00:00", IsAllDay: false,
            CalendarName: null, Provider: "google", IsCompleted: null, TaskListId: null, HtmlLink: null),
    ];

    // --- NeedsTokenRefresh boundary ---

    [Fact]
    public void NeedsTokenRefresh_ExactlyAtThirtySecondBoundary_ReturnsTrue()
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddSeconds(30);

        Assert.True(GoogleCalendarProvider.NeedsTokenRefresh(expiresAt, now));
    }

    [Fact]
    public void NeedsTokenRefresh_OneTickInsideBoundary_ReturnsFalse()
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddSeconds(30).AddTicks(1);

        Assert.False(GoogleCalendarProvider.NeedsTokenRefresh(expiresAt, now));
    }

    [Fact]
    public void NeedsTokenRefresh_WellPastExpiry_ReturnsTrue()
    {
        var now = DateTimeOffset.UtcNow;

        Assert.True(GoogleCalendarProvider.NeedsTokenRefresh(now.AddHours(-1), now));
    }

    [Fact]
    public void NeedsTokenRefresh_WellBeforeExpiry_ReturnsFalse()
    {
        var now = DateTimeOffset.UtcNow;

        Assert.False(GoogleCalendarProvider.NeedsTokenRefresh(now.AddHours(1), now));
    }

    // --- GetItemsAsync cache/token paths (no network) ---

    [Fact]
    public async Task GetItemsAsync_FreshCache_ReturnsCachedItemsWithoutTouchingToken()
    {
        using var db = new TempDatabase();
        var (provider, store) = MakeProvider(db);
        var items = SampleItems();
        store.SaveItemsCache("google", JsonSerializer.Serialize(items, JsonOpts));

        var result = (await provider.GetItemsAsync(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(14))).ToList();

        var item = Assert.Single(result);
        Assert.Equal("evt-1", item.Id);
        Assert.False(store.HasToken("google")); // never seeded — proves the token path was never reached
    }

    [Fact]
    public async Task GetItemsAsync_FreshCache_IgnoresRequestedDateRange()
    {
        // Documents a real quirk found while writing these tests: the cache check runs
        // before any comparison against the requested from/to, so a fresh cache entry is
        // served regardless of what range is actually asked for.
        using var db = new TempDatabase();
        var (provider, store) = MakeProvider(db);
        var items = SampleItems();
        store.SaveItemsCache("google", JsonSerializer.Serialize(items, JsonOpts));

        var farFuture = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var result = (await provider.GetItemsAsync(farFuture, farFuture.AddDays(14))).ToList();

        Assert.Single(result);
    }

    [Fact]
    public async Task GetItemsAsync_StaleCacheNoToken_ReturnsEmptyList()
    {
        using var db = new TempDatabase();
        var (provider, _) = MakeProvider(db);
        SeedCacheAt(db, "google", JsonSerializer.Serialize(SampleItems(), JsonOpts), DateTimeOffset.UtcNow.AddMinutes(-10));

        var result = await provider.GetItemsAsync(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(14));

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetItemsAsync_NoCacheNoToken_ReturnsEmptyList()
    {
        using var db = new TempDatabase();
        var (provider, _) = MakeProvider(db);

        var result = await provider.GetItemsAsync(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(14));

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetItemsAsync_CacheExactlyAtFiveMinuteBoundary_IsTreatedAsStale()
    {
        using var db = new TempDatabase();
        var (provider, _) = MakeProvider(db);
        SeedCacheAt(db, "google", JsonSerializer.Serialize(SampleItems(), JsonOpts), DateTimeOffset.UtcNow.AddMinutes(-5));

        var result = await provider.GetItemsAsync(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(14));

        Assert.Empty(result);
    }

    // --- IsAuthenticated / Disconnect ---

    [Fact]
    public void IsAuthenticated_NoTokenRow_ReturnsFalse()
    {
        using var db = new TempDatabase();
        var (provider, _) = MakeProvider(db);

        Assert.False(provider.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_TokenRowExists_ReturnsTrue()
    {
        using var db = new TempDatabase();
        var (provider, store) = MakeProvider(db);
        store.SaveToken("google", "access", "refresh", DateTimeOffset.UtcNow.AddHours(1));

        Assert.True(provider.IsAuthenticated);
    }

    [Fact]
    public void Disconnect_ClearsTokenAndCache()
    {
        using var db = new TempDatabase();
        var (provider, store) = MakeProvider(db);
        store.SaveToken("google", "access", "refresh", DateTimeOffset.UtcNow.AddHours(1));
        store.SaveItemsCache("google", "[]");

        provider.Disconnect();

        Assert.False(store.HasToken("google"));
        Assert.Null(store.LoadItemsCache("google"));
    }
}
