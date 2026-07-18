using System.Net;
using Rss.Tests.Helpers;
using Xunit;

namespace Rss.Tests;

public sealed class RssFetcherTests
{
    private const string AtomFeed = """
    <?xml version="1.0" encoding="utf-8"?>
    <feed xmlns="http://www.w3.org/2005/Atom">
      <title>Example Atom</title>
      <entry>
        <title>First Post</title>
        <link rel="self" href="https://example.com/self"/>
        <link rel="alternate" href="https://example.com/1"/>
        <summary>Summary one</summary>
        <published>2026-06-01T10:00:00Z</published>
      </entry>
    </feed>
    """;

    private const string Rss2Feed = """
    <?xml version="1.0"?>
    <rss version="2.0">
      <channel>
        <title>Example RSS</title>
        <item>
          <title>Item One</title>
          <link>https://example.com/a</link>
          <description>Desc A</description>
          <pubDate>Mon, 01 Jun 2026 10:00:00 GMT</pubDate>
        </item>
      </channel>
    </rss>
    """;

    private static RssFetcher MakeFetcher(string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new FakeHttpMessageHandler(body, status);
        var http = new HttpClient(handler);
        return new RssFetcher(http);
    }

    [Fact]
    public async Task FetchAsync_AtomFeed_ParsesTitleAndEntries()
    {
        var fetcher = MakeFetcher(AtomFeed);

        var result = await fetcher.FetchAsync("https://example.com/feed");

        Assert.NotNull(result);
        Assert.Equal("Example Atom", result!.Value.FeedTitle);
        var article = Assert.Single(result.Value.Articles);
        Assert.Equal("First Post", article.Title);
        // The self link must be skipped in favour of the alternate link.
        Assert.Equal("https://example.com/1", article.Link);
        Assert.Equal("Summary one", article.Description);
        Assert.Equal("2026-06-01T10:00:00Z", article.PublishedAt);
    }

    [Fact]
    public async Task FetchAsync_Rss2Feed_ParsesChannelAndItems()
    {
        var fetcher = MakeFetcher(Rss2Feed);

        var result = await fetcher.FetchAsync("https://example.com/feed");

        Assert.NotNull(result);
        Assert.Equal("Example RSS", result!.Value.FeedTitle);
        var article = Assert.Single(result.Value.Articles);
        Assert.Equal("Item One", article.Title);
        Assert.Equal("https://example.com/a", article.Link);
        Assert.Equal("Desc A", article.Description);
    }

    [Fact]
    public async Task FetchAsync_AtomEntryMissingOptionalFields_DefaultsToEmpty()
    {
        const string feed = """
        <?xml version="1.0" encoding="utf-8"?>
        <feed xmlns="http://www.w3.org/2005/Atom">
          <title>Bare</title>
          <entry><title>Only Title</title></entry>
        </feed>
        """;
        var fetcher = MakeFetcher(feed);

        var result = await fetcher.FetchAsync("https://example.com/feed");

        Assert.NotNull(result);
        var article = Assert.Single(result!.Value.Articles);
        Assert.Equal("Only Title", article.Title);
        Assert.Equal("", article.Link);
        Assert.Null(article.Description);
        Assert.Null(article.PublishedAt);
    }

    [Fact]
    public async Task FetchAsync_MalformedXml_ReturnsNull()
    {
        var fetcher = MakeFetcher("not-xml <broken");

        Assert.Null(await fetcher.FetchAsync("https://example.com/feed"));
    }

    [Fact]
    public async Task FetchAsync_HttpError_ReturnsNull()
    {
        var fetcher = MakeFetcher("", HttpStatusCode.ServiceUnavailable);

        Assert.Null(await fetcher.FetchAsync("https://example.com/feed"));
    }
}
