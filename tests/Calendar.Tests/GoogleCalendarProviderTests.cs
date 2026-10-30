using System.Text.Json;
using Calendar.Providers.Google;
using Calendar.Records;
using Hearth.TestUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using ServiceDefaults;

namespace Calendar.Tests;

public sealed class GoogleCalendarProviderTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 7, 20, 12, 0, 0, TimeSpan.Zero);

    // The window /calendar/items asks for: yesterday at UTC midnight to 60 days ahead
    private static readonly DateTimeOffset From = new(2026, 7, 19, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset To = From.AddDays(61);

    private static readonly string ItemsJson = JsonSerializer.Serialize(new List<CalendarItem>
    {
        new(Kind: "event", Id: "evt-1", Title: "Standup", Description: null, Location: null,
            Start: "2026-07-20T09:00:00+00:00", End: "2026-07-20T09:30:00+00:00", IsAllDay: false,
            CalendarName: null, Provider: "google", IsCompleted: null, TaskListId: null, HtmlLink: null),
    }, HearthJson.SnakeCaseLower);

    private readonly TempDatabase _db = new();
    // Starts before Now so CacheItems can save "in the past" (the fake clock only moves forward)
    private readonly FakeTimeProvider _clock = new(Now.AddHours(-1));
    private readonly CalendarStore _store;
    private readonly GoogleCalendarProvider _provider;

    public GoogleCalendarProviderTests()
    {
        _store = new CalendarStore(_db.Db, TestDataProtection.Provider, _clock);
        _store.Migrate();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GOOGLE_CLIENT_ID"] = "test-client-id",
                ["GOOGLE_CLIENT_SECRET"] = "test-client-secret",
                ["GOOGLE_REDIRECT_URI"] = "https://example.com/calendar/google/callback",
            })
            .Build();

        var authService = new GoogleAuthService(_store, config, _clock);
        _provider = new GoogleCalendarProvider(_store, authService, _clock, NullLogger<GoogleCalendarProvider>.Instance);
    }

    public void Dispose() => _db.Dispose();

    /// <summary>Caches the sample items for <see cref="From"/>–<see cref="To"/>, <paramref name="age"/> ago.</summary>
    private void CacheItems(TimeSpan age, DateTimeOffset? from = null)
    {
        _clock.SetUtcNow(Now - age);
        _store.SaveItemsCache("google", GoogleCalendarProvider.CacheRange(from ?? From, To), ItemsJson);
        _clock.SetUtcNow(Now);
    }

    // --- NeedsTokenRefresh boundary ---

    [Theory]
    [InlineData(-3600, true)]  // well past expiry
    [InlineData(30, true)]     // exactly at the 30 s margin
    [InlineData(30.0000001, false)]
    [InlineData(3600, false)]  // well before expiry
    public void NeedsTokenRefresh_SecondsToExpiry_RefreshesWithinThirtySeconds(double seconds, bool expected)
    {
        Assert.Equal(expected, GoogleCalendarProvider.NeedsTokenRefresh(Now.AddTicks((long)(seconds * TimeSpan.TicksPerSecond)), Now));
    }

    // --- GetItemsAsync cache/token paths (no network) ---
    // No token is saved, so anything not served from the cache comes back empty.

    [Fact]
    public async Task GetItemsAsync_FreshCacheForTheRange_ReturnsCachedItems()
    {
        CacheItems(TimeSpan.FromMinutes(1));

        var item = Assert.Single(await _provider.GetItemsAsync(From, To));

        Assert.Equal("evt-1", item.Id);
    }

    [Fact]
    public async Task GetItemsAsync_FreshCacheForAnotherRange_IsNotServed()
    {
        // Cached at 23:59 for yesterday's window, asked at 00:00 for today's
        CacheItems(TimeSpan.FromMinutes(1), from: From.AddDays(-1));

        Assert.Empty(await _provider.GetItemsAsync(From, To));
    }

    [Theory]
    [InlineData(299, 1)] // just inside the 5-minute TTL
    [InlineData(300, 0)] // exactly 5 minutes: stale
    [InlineData(600, 0)]
    public async Task GetItemsAsync_CacheAge_ServedOnlyUnderFiveMinutes(int ageSeconds, int expected)
    {
        CacheItems(TimeSpan.FromSeconds(ageSeconds));

        Assert.Equal(expected, (await _provider.GetItemsAsync(From, To)).Count());
    }

    [Fact]
    public async Task GetItemsAsync_NoCacheNoToken_ReturnsEmptyList()
    {
        Assert.Empty(await _provider.GetItemsAsync(From, To));
    }

    // --- IsAuthenticated / Disconnect ---

    [Fact]
    public void IsAuthenticated_FollowsTheStoredToken()
    {
        Assert.False(_provider.IsAuthenticated);

        _store.SaveToken("google", "access", "refresh", Now.AddHours(1));

        Assert.True(_provider.IsAuthenticated);
    }

    [Fact]
    public void Disconnect_ClearsTokenAndCache()
    {
        _store.SaveToken("google", "access", "refresh", Now.AddHours(1));
        CacheItems(TimeSpan.Zero);

        _provider.Disconnect();

        Assert.False(_store.HasToken("google"));
        Assert.Null(_store.LoadItemsCache("google"));
    }
}
