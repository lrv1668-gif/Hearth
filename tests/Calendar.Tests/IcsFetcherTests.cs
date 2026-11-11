using System.Net;
using Calendar.Providers.Ics;
using Hearth.TestUtilities;
using ServiceDefaults;

namespace Calendar.Tests;

public sealed class IcsFetcherTests
{
    [Theory]
    [InlineData("webcal://p01-caldav.icloud.com/published/2/abc", "https://p01-caldav.icloud.com/published/2/abc")]
    [InlineData("WEBCAL://example.com/cal.ics", "https://example.com/cal.ics")]
    [InlineData("webcals://example.com/cal.ics", "https://example.com/cal.ics")]
    [InlineData("  https://example.com/cal.ics  ", "https://example.com/cal.ics")]
    [InlineData("http://example.com/cal.ics", "http://example.com/cal.ics")]
    public void NormalizeUrl_CalendarLinks_BecomeHttpUrls(string input, string expected) =>
        Assert.Equal(expected, IcsFetcher.NormalizeUrl(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a link")]
    [InlineData("ftp://example.com/cal.ics")]
    [InlineData("file:///etc/passwd")]
    [InlineData("/published/2/abc")]
    public void NormalizeUrl_AnythingElse_IsNull(string? input) =>
        Assert.Null(IcsFetcher.NormalizeUrl(input));

    [Theory]
    [InlineData("http://127.0.0.1/cal.ics")]
    [InlineData("http://192.168.1.10/cal.ics")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    public async Task FetchAsync_NonPublicAddress_IsRefused(string url)
    {
        var fetcher = new IcsFetcher(new FeedUrlValidator());

        await Assert.ThrowsAsync<IcsFeedException>(() => fetcher.FetchAsync(url));
    }

    [Fact]
    public async Task FetchAsync_HostResolvingToThePrivateNetwork_IsRefused()
    {
        var fetcher = new IcsFetcher(new FeedUrlValidator(_ => Task.FromResult<IPAddress[]>([IPAddress.Parse("10.0.0.5")])));

        await Assert.ThrowsAsync<IcsFeedException>(() => fetcher.FetchAsync("https://calendar.example/cal.ics"));
    }

    [Fact]
    public async Task FetchPinnedAsync_ReachableFeed_ReturnsItsText()
    {
        using var server = new FeedServer(IcsSamples.Daily);

        // The hostname is never resolved: the request goes to the pinned address
        var text = await IcsFetcher.FetchPinnedAsync(
            $"http://calendar.invalid:{server.Port}/cal.ics", IPAddress.Loopback, CancellationToken.None);

        Assert.Contains("BEGIN:VCALENDAR", text);
        Assert.Equal(1, server.Requests);
    }

    [Fact]
    public async Task FetchPinnedAsync_ServerError_Throws()
    {
        using var server = new FeedServer(IcsSamples.Daily) { Failing = true };

        await Assert.ThrowsAsync<HttpRequestException>(() => IcsFetcher.FetchPinnedAsync(
            $"http://calendar.invalid:{server.Port}/cal.ics", IPAddress.Loopback, CancellationToken.None));
    }
}
