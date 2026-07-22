using System.Net;
using System.Net.Sockets;
using System.Text;
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

    [Fact]
    public void ParseFeed_AtomFeed_ParsesTitleAndEntries()
    {
        var (feedTitle, articles) = RssFetcher.ParseFeed("https://example.com/feed", AtomFeed);

        Assert.Equal("Example Atom", feedTitle);
        var article = Assert.Single(articles);
        Assert.Equal("First Post", article.Title);
        // The self link must be skipped in favour of the alternate link.
        Assert.Equal("https://example.com/1", article.Link);
        Assert.Equal("Summary one", article.Description);
        Assert.Equal("2026-06-01T10:00:00Z", article.PublishedAt);
    }

    [Fact]
    public void ParseFeed_Rss2Feed_ParsesChannelAndItems()
    {
        var (feedTitle, articles) = RssFetcher.ParseFeed("https://example.com/feed", Rss2Feed);

        Assert.Equal("Example RSS", feedTitle);
        var article = Assert.Single(articles);
        Assert.Equal("Item One", article.Title);
        Assert.Equal("https://example.com/a", article.Link);
        Assert.Equal("Desc A", article.Description);
    }

    [Fact]
    public void ParseFeed_AtomEntryMissingOptionalFields_DefaultsToEmpty()
    {
        const string feed = """
        <?xml version="1.0" encoding="utf-8"?>
        <feed xmlns="http://www.w3.org/2005/Atom">
          <title>Bare</title>
          <entry><title>Only Title</title></entry>
        </feed>
        """;

        var (_, articles) = RssFetcher.ParseFeed("https://example.com/feed", feed);

        var article = Assert.Single(articles);
        Assert.Equal("Only Title", article.Title);
        Assert.Equal("", article.Link);
        Assert.Null(article.Description);
        Assert.Null(article.PublishedAt);
    }

    [Fact]
    public void ParseFeed_MalformedXml_Throws()
    {
        Assert.ThrowsAny<Exception>(() => RssFetcher.ParseFeed("https://example.com/feed", "not-xml <broken"));
    }

    [Fact]
    public async Task FetchAsync_ConnectionRefused_ReturnsNull()
    {
        var fetcher = new RssFetcher();

        // Port 1 (tcpmux) on loopback: nothing listens there, so the connect fails
        // immediately (refused) instead of hanging on a timeout.
        var result = await fetcher.FetchAsync("https://example.com:1/feed", IPAddress.Loopback);

        Assert.Null(result);
    }

    [Fact]
    public async Task FetchAsync_PinnedAddressServesFeed_ConnectsAndParsesResponse()
    {
        // Exercises the actual ConnectCallback/socket wiring end-to-end (not just ParseFeed):
        // the URL's host is a domain that doesn't resolve here, but FetchAsync must connect to
        // the pinned loopback address rather than the host, and still parse what comes back.
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var serverTask = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            using var stream = client.GetStream();

            using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
            while (!string.IsNullOrEmpty(await reader.ReadLineAsync())) { } // drain request headers

            var body = Encoding.UTF8.GetBytes(AtomFeed);
            var header = Encoding.ASCII.GetBytes(
                $"HTTP/1.1 200 OK\r\nContent-Type: application/xml\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(header);
            await stream.WriteAsync(body);
        });

        var fetcher = new RssFetcher();
        var result = await fetcher
            .FetchAsync("http://this-host-does-not-resolve.invalid:" + port + "/feed", IPAddress.Loopback)
            .WaitAsync(TimeSpan.FromSeconds(10));

        await serverTask.WaitAsync(TimeSpan.FromSeconds(5));
        listener.Stop();

        Assert.NotNull(result);
        Assert.Equal("Example Atom", result!.Value.FeedTitle);
        var article = Assert.Single(result.Value.Articles);
        Assert.Equal("First Post", article.Title);
    }
}
