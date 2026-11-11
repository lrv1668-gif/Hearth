using Hearth.TestUtilities;
using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Rss.Records;
using ZiggyCreatures.Caching.Fusion;

namespace Rss.Tests;

public sealed class HeadlinesTests : IDisposable
{
    private const string Feed = """
    <?xml version="1.0"?>
    <rss version="2.0"><channel><title>Example RSS</title>
      <item><title>Older</title><link>https://example.com/a</link><pubDate>Tue, 03 Nov 2026 08:00:00 GMT</pubDate></item>
      <item><title>Newer</title><link>https://example.com/b</link><pubDate>Tue, 03 Nov 2026 09:30:00 +0000</pubDate></item>
    </channel></rss>
    """;

    private readonly FeedServer _server = new(Feed);
    private readonly FusionCache _cache = new(new FusionCacheOptions());
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 4, 9, 10, 0, TimeSpan.Zero));
    private readonly Headlines _headlines;

    public HeadlinesTests() =>
        _headlines = new Headlines(new RssFeeds(new RssFetcher(), _cache, NullLogger<RssFeeds>.Instance), _cache, _clock);

    public void Dispose()
    {
        _cache.Dispose();
        _server.Dispose();
    }

    private (string, IPAddress)[] Feeds => [($"http://feed.invalid:{_server.Port}/feed", IPAddress.Loopback)];

    private static Feed FeedOf(string title, params (string Title, string? Published)[] items) =>
        new(title, items.Select(i => new ArticleItem(i.Title, "https://example.com", null, i.Published)).ToList());

    [Fact]
    public void Pick_SortsByParsedDate_NotTheRawString()
    {
        var feed = FeedOf("A", ("Monday", "Mon, 02 Nov 2026 12:00:00 GMT"), ("Tuesday", "Tue, 03 Nov 2026 06:00:00 GMT"));

        var picked = Headlines.Pick([feed], 2);

        Assert.Equal(["Tuesday", "Monday"], picked.Select(h => h.Title));
        Assert.Equal("2026-11-03T06:00:00.0000000+00:00", picked[0].PublishedAt);
    }

    [Fact]
    public void Pick_TakesFromEachFeedInTurn_FreshestFeedFirst()
    {
        var busy = FeedOf("Busy", ("B1", "2026-11-03T08:00:00Z"), ("B2", "2026-11-03T07:00:00Z"), ("B3", "2026-11-03T06:00:00Z"));
        var quiet = FeedOf("Quiet", ("Q1", "2026-11-03T09:00:00Z"));

        var picked = Headlines.Pick([busy, quiet], 3);

        Assert.Equal(["Q1", "B1", "B2"], picked.Select(h => h.Title));
        Assert.Equal("Quiet", picked[0].FeedTitle);
    }

    [Fact]
    public void Pick_UndatedItems_ComeAfterDatedOnesInFeedOrder()
    {
        var feed = FeedOf("A", ("First undated", null), ("Dated", "2026-11-03T08:00:00Z"), ("Second undated", "garbage"));

        var picked = Headlines.Pick([feed], 3);

        Assert.Equal(["Dated", "First undated", "Second undated"], picked.Select(h => h.Title));
        Assert.Null(picked[1].PublishedAt);
    }

    [Fact]
    public void Pick_SameTitleOnTwoFeeds_KeepsTheFirstOnly()
    {
        var wire = FeedOf("Wire", ("Storm hits coast", "2026-11-03T09:00:00Z"), ("Markets rally", "2026-11-03T08:00:00Z"));
        var paper = FeedOf("Paper", ("storm hits coast ", "2026-11-03T08:30:00Z"), ("Local fair opens", "2026-11-03T07:00:00Z"));

        var picked = Headlines.Pick([wire, paper], 4);

        Assert.Equal(["Storm hits coast", "Markets rally", "Local fair opens"], picked.Select(h => h.Title));
        Assert.Equal("Wire", picked[0].FeedTitle);
    }

    [Fact]
    public async Task GetAsync_SameHour_HoldsTheEditionEvenWhenTheFeedRefreshes()
    {
        var first = await _headlines.GetAsync(Feeds, 5);
        _clock.Advance(TimeSpan.FromMinutes(40));
        await _cache.ExpireAsync($"rss:{Feeds[0].Item1}");

        var again = await _headlines.GetAsync(Feeds, 5);

        Assert.Equal(["Newer", "Older"], first.Select(h => h.Title));
        Assert.Same(first, again);
        Assert.Equal(1, _server.Requests);
    }

    [Fact]
    public async Task GetAsync_NextHour_PicksANewEdition()
    {
        await _headlines.GetAsync(Feeds, 5);
        _clock.Advance(TimeSpan.FromMinutes(55));

        var next = await _headlines.GetAsync(Feeds, 1);

        Assert.Equal("Newer", Assert.Single(next).Title);
    }
}
