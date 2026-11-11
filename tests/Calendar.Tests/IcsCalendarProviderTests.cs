using Calendar.Providers.Ics;
using Hearth.TestUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using ServiceDefaults;
using ZiggyCreatures.Caching.Fusion;

namespace Calendar.Tests;

public sealed class IcsCalendarProviderTests : IDisposable
{
    private const string Link = "https://p01-caldav.icloud.com/published/2/secret-token";
    private static readonly DateTimeOffset From = new(2026, 11, 9, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset To = From.AddDays(61);

    private readonly TempDatabase _db = new();
    private readonly FusionCache _cache = new(new FusionCacheOptions());
    private readonly FakeIcsSource _source = new(IcsSamples.Daily);
    private readonly FakeLogger<IcsCalendarProvider> _logger = new();
    private readonly IcsFeedStore _store;
    private readonly IcsCalendarProvider _provider;

    public IcsCalendarProviderTests()
    {
        _store = new IcsFeedStore(_db.Db, TestDataProtection.Provider, new FakeTimeProvider());
        _store.Migrate();

        // No DISPLAY_URL: the home zone comes from TZ
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["TZ"] = "America/Denver" })
            .Build();
        var home = new HomeLocationClient(new HttpClient(), _cache, config, NullLogger<HomeLocationClient>.Instance);

        _provider = new IcsCalendarProvider(_store, _source, _cache, home, _logger);
    }

    public void Dispose()
    {
        _cache.Dispose();
        _db.Dispose();
    }

    [Fact]
    public void IsConnected_FollowsWhetherAnyCalendarIsSubscribed()
    {
        Assert.False(_provider.IsConnected);

        _store.Add("Family", Link);

        Assert.True(_provider.IsConnected);
    }

    [Fact]
    public async Task GetItemsAsync_NoFeeds_ReturnsEmptyWithoutFetching()
    {
        Assert.Empty(await _provider.GetItemsAsync(From, To));
        Assert.Equal(0, _source.Requests);
    }

    [Fact]
    public async Task GetItemsAsync_SubscribedFeed_ReturnsItsEventsInTheHomeZone()
    {
        _store.Add("Family", Link);

        var items = (await _provider.GetItemsAsync(From, To)).ToList();

        Assert.Equal(61, items.Count);
        Assert.All(items, i => Assert.Equal("Family", i.CalendarName));
        Assert.All(items, i => Assert.Equal("ics", i.Provider));
        Assert.Equal("2026-11-09T05:00:00.0000000-07:00", items[0].Start);
        Assert.Equal(Link, _source.LastUrl);
    }

    [Fact]
    public async Task GetItemsAsync_SeveralFeeds_MergesThemUnderTheirOwnNames()
    {
        _store.Add("Family", Link);
        _store.Add("School", "https://example.com/school.ics");

        var items = await _provider.GetItemsAsync(From, To);

        Assert.Equal(["Family", "School"], items.Select(i => i.CalendarName!).Distinct().Order().ToArray());
        Assert.Equal(2, _source.Requests);
    }

    [Fact]
    public async Task GetItemsAsync_SecondRead_IsServedFromTheCache()
    {
        _store.Add("Family", Link);

        await _provider.GetItemsAsync(From, To);
        await _provider.GetItemsAsync(From, To);

        Assert.Equal(1, _source.Requests);
    }

    [Fact]
    public async Task RefreshAsync_NextRead_FetchesAgain()
    {
        _store.Add("Family", Link);
        await _provider.GetItemsAsync(From, To);

        await _provider.RefreshAsync();
        await _provider.GetItemsAsync(From, To);

        Assert.Equal(2, _source.Requests);
    }

    [Fact]
    public async Task GetItemsAsync_RefreshFails_KeepsServingTheLastGoodCopy()
    {
        _store.Add("Family", Link);
        await _provider.GetItemsAsync(From, To);

        _source.Failing = true;
        await _provider.RefreshAsync();
        var items = await _provider.GetItemsAsync(From, To);

        Assert.Equal(61, items.Count());
    }

    [Fact]
    public async Task GetItemsAsync_NeverFetched_ReturnsEmptyAndLogsWithoutTheLink()
    {
        _store.Add("Family", Link);
        _source.Failing = true;

        Assert.Empty(await _provider.GetItemsAsync(From, To));

        Assert.Contains("Family", _logger.LatestRecord.Message);
        Assert.DoesNotContain("secret-token", _logger.LatestRecord.Message);
    }

    [Fact]
    public async Task GetItemsAsync_OneFeedBroken_StillReturnsTheOthers()
    {
        _store.Add("Family", Link);
        await _provider.GetItemsAsync(From, To);
        _store.Add("Broken", "https://example.com/broken.ics");
        _source.Ics = "<html>gone</html>";

        var items = await _provider.GetItemsAsync(From, To);

        Assert.Equal(["Family"], items.Select(i => i.CalendarName!).Distinct().ToArray());
    }

    [Fact]
    public async Task AddFeedAsync_WebcalLinkAndBlankName_StoresHttpsUnderTheCalendarsOwnName()
    {
        var feed = await _provider.AddFeedAsync("  ", "webcal://p01-caldav.icloud.com/published/2/secret-token");

        Assert.Equal("Family", feed.Name);
        Assert.Equal(Link, feed.Url);
        Assert.Equal(feed, Assert.Single(_store.List()));
    }

    [Fact]
    public async Task AddFeedAsync_GivenName_WinsAndIsTrimmedToLength()
    {
        var feed = await _provider.AddFeedAsync("  " + new string('x', 80) + "  ", Link);

        Assert.Equal(new string('x', IcsCalendarProvider.MaxNameLength), feed.Name);
    }

    [Fact]
    public async Task AddFeedAsync_UnnamedCalendarAndBlankName_IsCalledCalendar()
    {
        _source.Ics = IcsSamples.Feed("", name: null);

        Assert.Equal("Calendar", (await _provider.AddFeedAsync(null, Link)).Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a link")]
    [InlineData("ftp://example.com/cal.ics")]
    public async Task AddFeedAsync_NotALink_IsRefusedWithoutFetching(string? url)
    {
        await Assert.ThrowsAsync<IcsFeedException>(() => _provider.AddFeedAsync("Family", url));

        Assert.Equal(0, _source.Requests);
        Assert.Empty(_store.List());
    }

    [Fact]
    public async Task AddFeedAsync_LinkThatCannotBeRead_IsRefused()
    {
        _source.Failing = true;

        var ex = await Assert.ThrowsAsync<IcsFeedException>(() => _provider.AddFeedAsync("Family", Link));

        Assert.Contains("Couldn't read that link", ex.Message);
        Assert.Empty(_store.List());
        Assert.DoesNotContain("secret-token", _logger.LatestRecord.Message);
    }

    [Fact]
    public async Task AddFeedAsync_LinkThatIsNotACalendar_IsRefused()
    {
        _source.Ics = "<html>Sign in</html>";

        await Assert.ThrowsAsync<IcsFeedException>(() => _provider.AddFeedAsync("Family", Link));

        Assert.Empty(_store.List());
    }

    [Fact]
    public async Task AddFeedAsync_AtTheLimit_IsRefused()
    {
        for (var i = 0; i < IcsFeedStore.MaxFeeds; i++)
            _store.Add($"Calendar {i}", $"https://example.com/{i}.ics");

        await Assert.ThrowsAsync<IcsFeedException>(() => _provider.AddFeedAsync("One more", Link));
    }

    [Fact]
    public async Task RemoveFeedAsync_SubscribedFeed_DropsItAndItsItems()
    {
        var feed = _store.Add("Family", Link);
        await _provider.GetItemsAsync(From, To);

        Assert.True(await _provider.RemoveFeedAsync(feed.Id));

        Assert.False(_provider.IsConnected);
        Assert.Empty(await _provider.GetItemsAsync(From, To));
        Assert.False(await _provider.RemoveFeedAsync(feed.Id));
    }
}
