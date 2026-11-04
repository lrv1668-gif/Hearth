using System.Net;
using System.Net.Http.Json;
using Hearth.TestUtilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Display.Tests;

/// <summary>/display/household: the shared RSS feeds and transit stops.</summary>
public sealed class HouseholdEndpointTests : IDisposable
{
    private readonly TempDatabase _db = new();
    private readonly FakeHttpMessageHandler _renderer = new("png");
    private readonly WebApplicationFactory<Program> _factory;

    public HouseholdEndpointTests()
    {
        _factory = HearthApp.Create<Program>("display", _db, host =>
        {
            host.UseSetting("RENDERER_URL", "http://renderer:3000");
            host.ConfigureTestServices(services =>
                services.AddHttpClient<RendererClient>().ConfigurePrimaryHttpMessageHandler(() => _renderer));
        });
    }

    public void Dispose()
    {
        _factory.Dispose();
        _db.Dispose();
    }

    [Fact]
    public async Task Get_NothingSaved_Returns204()
    {
        var response = await _factory.CreateClient().GetAsync("/display/household");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Put_Valid_SavesAndGetReturnsItSnakeCased()
    {
        var client = _factory.CreateClient();

        var put = await client.PutAsJsonAsync("/display/household", new
        {
            rss_feeds = new[] { new { title = "BBC", url = "https://feeds.bbci.co.uk/news/rss.xml" } },
            train_stops = new[] { new { label = "Home", stop_key = "s-1", line_filter = new[] { "Red|Richmond|rail" } } },
        });
        var json = await client.GetStringAsync("/display/household");

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.Contains("\"stop_key\":\"s-1\"", json);
        Assert.Contains("\"line_filter\":[\"Red|Richmond|rail\"]", json);
        Assert.Contains("\"url\":\"https://feeds.bbci.co.uk/news/rss.xml\"", json);
    }

    [Theory]
    [InlineData("ftp://example.com/feed", "Home", "s-1")]
    [InlineData("not a url", "Home", "s-1")]
    [InlineData("https://example.com/feed", "", "s-1")]
    [InlineData("https://example.com/feed", "Home", " ")]
    public async Task Put_Invalid_Returns400(string url, string label, string stopKey)
    {
        var response = await _factory.CreateClient().PutAsJsonAsync("/display/household", new
        {
            rss_feeds = new[] { new { title = "Feed", url } },
            train_stops = new[] { new { label, stop_key = stopKey } },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_TooManyFeeds_Returns400()
    {
        var feeds = Enumerable.Range(0, 51).Select(i => new { title = $"F{i}", url = $"https://example.com/{i}" });

        var response = await _factory.CreateClient().PutAsJsonAsync("/display/household",
            new { rss_feeds = feeds, train_stops = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
