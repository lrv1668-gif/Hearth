using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Rss.Records;
using ServiceDefaults;
using Xunit;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace Rss.Tests;

public sealed class RssFeedsTests : IDisposable
{
    private const string Feed = """
    <?xml version="1.0"?>
    <rss version="2.0"><channel><title>Example RSS</title>
      <item><title>Item One</title><link>https://example.com/a</link></item>
    </channel></rss>
    """;

    private readonly FeedServer _server = new();
    private readonly FusionCache _cache = new(new FusionCacheOptions());
    private readonly RssFeeds _feeds;

    public RssFeedsTests() =>
        _feeds = new RssFeeds(new RssFetcher(), _cache, NullLogger<RssFeeds>.Instance);

    public void Dispose()
    {
        _cache.Dispose();
        _server.Dispose();
    }

    private string Url => $"http://feed.invalid:{_server.Port}/feed";

    [Fact]
    public async Task GetAsync_CalledTwice_FetchesOnce()
    {
        var first = await _feeds.GetAsync(Url, IPAddress.Loopback);
        var second = await _feeds.GetAsync(Url, IPAddress.Loopback);

        Assert.Equal("Example RSS", first!.FeedTitle);
        Assert.Equal("Item One", Assert.Single(second!.Articles).Title);
        Assert.Equal(1, _server.Requests);
    }

    [Fact]
    public async Task GetAsync_CalledTogether_FetchesOnce()
    {
        var feeds = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => _feeds.GetAsync(Url, IPAddress.Loopback)));

        Assert.All(feeds, f => Assert.Equal("Example RSS", f!.FeedTitle));
        Assert.Equal(1, _server.Requests);
    }

    [Fact]
    public async Task GetAsync_RefreshFails_ServesLastGoodCopy()
    {
        await _feeds.GetAsync(Url, IPAddress.Loopback);
        _server.Failing = true;
        // Due for a refresh, as after Duration
        await _cache.ExpireAsync($"rss:{Url}");

        var feed = await _feeds.GetAsync(Url, IPAddress.Loopback);

        Assert.Equal("Example RSS", feed!.FeedTitle);
        Assert.Equal(2, _server.Requests);
    }

    [Fact]
    public async Task GetAsync_NeverFetched_ReturnsNull()
    {
        _server.Failing = true;

        Assert.Null(await _feeds.GetAsync(Url, IPAddress.Loopback));
    }

    [Fact]
    public void Feed_ThroughTheL2Serializer_RoundTrips()
    {
        // What the SQLite L2 holds across a restart
        var serializer = new FusionCacheSystemTextJsonSerializer(HearthJson.SnakeCaseLower);
        var feed = new Feed("Example RSS", [new ArticleItem("Item One", "https://example.com/a", "Desc", "Mon")]);

        var back = serializer.Deserialize<Feed>(serializer.Serialize(feed))!;

        Assert.Equal(feed.FeedTitle, back.FeedTitle);
        Assert.Equal(feed.Articles, back.Articles);
    }

    /// <summary>A loopback HTTP server answering every request with <see cref="Feed"/>, or a 500 while failing.</summary>
    private sealed class FeedServer : IDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _stop = new();
        private int _requests;

        public FeedServer()
        {
            _listener.Start();
            _ = Task.Run(ServeAsync);
        }

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;
        public int Requests => Volatile.Read(ref _requests);
        public volatile bool Failing;

        private async Task ServeAsync()
        {
            while (!_stop.IsCancellationRequested)
            {
                TcpClient client;
                try { client = await _listener.AcceptTcpClientAsync(_stop.Token); }
                catch (OperationCanceledException) { return; }
                _ = Task.Run(async () =>
                {
                    using (client)
                    {
                        var stream = client.GetStream();
                        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
                        while (!string.IsNullOrEmpty(await reader.ReadLineAsync())) { } // drain request headers
                        Interlocked.Increment(ref _requests);

                        var (status, body) = Failing ? ("500 Internal Server Error", "") : ("200 OK", Feed);
                        var bytes = Encoding.UTF8.GetBytes(body);
                        await stream.WriteAsync(Encoding.ASCII.GetBytes(
                            $"HTTP/1.1 {status}\r\nContent-Type: application/xml\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n"));
                        await stream.WriteAsync(bytes);
                    }
                });
            }
        }

        public void Dispose()
        {
            _stop.Cancel();
            _listener.Stop();
            _stop.Dispose();
        }
    }
}
