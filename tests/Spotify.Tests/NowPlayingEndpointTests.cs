using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using ServiceDefaults;
using Spotify.Records;
using ZiggyCreatures.Caching.Fusion;

namespace Spotify.Tests;

/// <summary>/spotify/now-playing: which Spotify answers count as disconnected, and the shared cache.</summary>
public sealed class NowPlayingEndpointTests() : SpotifyEndpointTests(PlayingJson)
{
    private const string PlayingJson = """
        {
          "is_playing": true,
          "progress_ms": 61000,
          "currently_playing_type": "track",
          "item": {
            "type": "track",
            "name": "Clair de Lune",
            "duration_ms": 300000,
            "artists": [{ "name": "Debussy" }],
            "album": { "name": "Suite bergamasque", "images": [{ "url": "https://img.example/a.jpg" }] }
          }
        }
        """;

    private HttpClient Connected() => Connected(AllScopes);

    [Fact]
    public async Task Get_NoSavedLogin_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/spotify/now-playing");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, _spotify.RequestCount);
    }

    [Fact]
    public async Task Get_Playing_ReturnsTrack()
    {
        var response = await Connected().GetAsync("/spotify/now-playing");

        response.EnsureSuccessStatusCode();
        var track = await response.Content.ReadFromJsonAsync<NowPlayingResponse>(HearthJson.SnakeCaseLower);
        Assert.Equal("Clair de Lune", track!.Title);
        Assert.Equal("Debussy", track.Artist);
        Assert.Equal(61000, track.ProgressMs);
    }

    [Fact]
    public async Task Get_NothingPlaying_Returns204()
    {
        _spotify.Status = HttpStatusCode.NoContent;
        _spotify.ResponseJson = "";

        var response = await Connected().GetAsync("/spotify/now-playing");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Get_SpotifyRejectsLogin_Returns401()
    {
        _spotify.Status = HttpStatusCode.Unauthorized;
        _spotify.ResponseJson = """{ "error": { "status": 401, "message": "The access token expired" } }""";

        var response = await Connected().GetAsync("/spotify/now-playing");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Get_SpotifyRateLimitedOrDown_Returns502NotDisconnected(HttpStatusCode upstream)
    {
        _spotify.Status = upstream;
        _spotify.ResponseJson = """{ "error": { "status": 0, "message": "try later" } }""";

        var response = await Connected().GetAsync("/spotify/now-playing");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task Get_RepeatedPolls_ShareOneSpotifyCall()
    {
        var client = Connected();

        for (var i = 0; i < 3; i++)
            (await client.GetAsync("/spotify/now-playing")).EnsureSuccessStatusCode();

        Assert.Equal(1, _spotify.RequestCount);
    }

    [Fact]
    public async Task Get_SpotifyFailsAfterAGoodRead_ServesTheLastTrack()
    {
        var client = Connected();
        (await client.GetAsync("/spotify/now-playing")).EnsureSuccessStatusCode();
        // The cached answer goes stale, and the refresh hits a Spotify outage
        await _factory.Services.GetRequiredService<IFusionCache>().ExpireAsync("spotify:now-playing");
        _spotify.Status = HttpStatusCode.ServiceUnavailable;

        var response = await client.GetAsync("/spotify/now-playing");

        response.EnsureSuccessStatusCode();
        var track = await response.Content.ReadFromJsonAsync<NowPlayingResponse>(HearthJson.SnakeCaseLower);
        Assert.Equal("Clair de Lune", track!.Title);
        Assert.Equal(2, _spotify.RequestCount);
    }
}
