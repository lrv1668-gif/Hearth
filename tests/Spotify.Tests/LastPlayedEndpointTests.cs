using System.Net;
using System.Net.Http.Json;
using Data.Abstractions;
using Hearth.TestUtilities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ServiceDefaults;
using Spotify.Records;
using Xunit;

namespace Spotify.Tests;

/// <summary>/spotify/recent and the reconnect flag on /spotify/status: the frame's Last Played card.</summary>
public sealed class LastPlayedEndpointTests : IDisposable
{
    private const string RecentPath = "/v1/me/player/recently-played";
    private const string TopArtistsPath = "/v1/me/top/artists";
    private const string TokenPath = "/api/token";

    private const string RecentJson = """
        {
          "items": [{
            "played_at": "2026-10-24T18:30:00.000Z",
            "track": {
              "type": "track",
              "name": "Clair de Lune",
              "artists": [{ "name": "Debussy" }],
              "album": {
                "name": "Suite bergamasque",
                "images": [
                  { "url": "https://img.example/640.jpg", "width": 640, "height": 640 },
                  { "url": "https://img.example/300.jpg", "width": 300, "height": 300 },
                  { "url": "https://img.example/64.jpg", "width": 64, "height": 64 }
                ]
              }
            }
          }]
        }
        """;

    private const string TopArtistsJson = """{ "items": [{ "name": "Nils Frahm" }] }""";

    private static readonly string AllScopes = string.Join(' ', SpotifyScopes.Required);

    private readonly TempDatabase _db = new();
    private readonly FakeHttpMessageHandler _spotify = new("{}");
    private readonly WebApplicationFactory<Program> _factory;

    public LastPlayedEndpointTests()
    {
        _spotify.ByPath[RecentPath] = (HttpStatusCode.OK, RecentJson);
        _spotify.ByPath[TopArtistsPath] = (HttpStatusCode.OK, TopArtistsJson);
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
        {
            host.UseSetting("SPOTIFY_CLIENT_ID", "test-client-id");
            host.UseSetting("SPOTIFY_CLIENT_SECRET", "test-client-secret");
            host.ConfigureTestServices(services =>
            {
                services.AddKeyedSingleton<IDatabase>("spotify", _db.Db);
                services.AddSingleton<IDataProtectionProvider>(TestDataProtection.Provider);
                services.AddHttpClient(SpotifyClientService.HttpClientName)
                    .ConfigurePrimaryHttpMessageHandler(() => _spotify);
            });
        });
    }

    public void Dispose()
    {
        _factory.Dispose();
        _db.Dispose();
    }

    private SpotifyStore Store => _factory.Services.GetRequiredService<SpotifyStore>();

    private HttpClient Connected(string? scope)
    {
        var client = _factory.CreateClient();
        Store.Save("access", "refresh", DateTime.UtcNow.AddHours(1), scope);
        return client;
    }

    private static async Task<LastPlayedResponse> Read(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LastPlayedResponse>(HearthJson.SnakeCaseLower))!;
    }

    [Fact]
    public async Task Get_NoSavedLogin_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/spotify/recent");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, _spotify.RequestCount);
    }

    [Fact]
    public async Task Get_LoginWithoutHistoryScopes_Returns403WithoutCallingSpotify()
    {
        var response = await Connected(scope: null).GetAsync("/spotify/recent");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, _spotify.RequestCount);
    }

    [Fact]
    public async Task Get_SpotifyRefusesScope_Returns403()
    {
        _spotify.ByPath[RecentPath] = (HttpStatusCode.Forbidden,
            """{ "error": { "status": 403, "message": "Insufficient client scope" } }""");

        var response = await Connected(AllScopes).GetAsync("/spotify/recent");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_History_ReturnsLatestTrackAndTopArtist()
    {
        var last = await Read(await Connected(AllScopes).GetAsync("/spotify/recent"));

        Assert.Equal("Clair de Lune", last.Track!.Title);
        Assert.Equal("Debussy", last.Track.Artist);
        Assert.Equal("https://img.example/300.jpg", last.Track.AlbumArtUrl);
        Assert.Equal(new DateTime(2026, 10, 24, 18, 30, 0, DateTimeKind.Utc), last.Track.PlayedAt.ToUniversalTime());
        Assert.Equal("Nils Frahm", last.TopArtist);
    }

    [Fact]
    public async Task Get_TopArtistFails_StillReturnsTheTrack()
    {
        _spotify.ByPath[TopArtistsPath] = (HttpStatusCode.ServiceUnavailable,
            """{ "error": { "status": 503, "message": "try later" } }""");

        var last = await Read(await Connected(AllScopes).GetAsync("/spotify/recent"));

        Assert.Equal("Clair de Lune", last.Track!.Title);
        Assert.Null(last.TopArtist);
    }

    [Fact]
    public async Task Get_NoHistory_Returns204()
    {
        _spotify.ByPath[RecentPath] = (HttpStatusCode.OK, """{ "items": [] }""");
        _spotify.ByPath[TopArtistsPath] = (HttpStatusCode.OK, """{ "items": [] }""");

        var response = await Connected(AllScopes).GetAsync("/spotify/recent");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Get_SpotifyDown_Returns502()
    {
        _spotify.ByPath[RecentPath] = (HttpStatusCode.ServiceUnavailable,
            """{ "error": { "status": 503, "message": "try later" } }""");

        var response = await Connected(AllScopes).GetAsync("/spotify/recent");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task Get_RepeatedRenders_ShareOneCallEach()
    {
        var client = Connected(AllScopes);

        for (var i = 0; i < 3; i++)
            (await client.GetAsync("/spotify/recent")).EnsureSuccessStatusCode();

        Assert.Equal(2, _spotify.RequestCount);
    }

    [Fact]
    public async Task Disconnect_ThenReconnect_DropsTheCachedHistory()
    {
        var client = Connected(AllScopes);
        (await client.GetAsync("/spotify/recent")).EnsureSuccessStatusCode();

        (await client.DeleteAsync("/spotify/auth")).EnsureSuccessStatusCode();
        Store.Save("access", "refresh", DateTime.UtcNow.AddHours(1), AllScopes);
        (await client.GetAsync("/spotify/recent")).EnsureSuccessStatusCode();

        Assert.Equal(4, _spotify.RequestCount);
    }

    [Fact]
    public async Task Get_TokenRefreshWithoutScope_KeepsTheStoredScope()
    {
        var client = _factory.CreateClient();
        Store.Save("access", "refresh", DateTime.UtcNow.AddHours(-1), AllScopes);
        _spotify.ByPath[TokenPath] = (HttpStatusCode.OK,
            """{ "access_token": "access-2", "token_type": "Bearer", "expires_in": 3600 }""");

        (await client.GetAsync("/spotify/recent")).EnsureSuccessStatusCode();

        var token = Store.Load()!;
        Assert.Equal("access-2", token.AccessToken);
        Assert.True(token.HasRequiredScopes);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("user-read-currently-playing user-read-playback-state", true)]
    [InlineData("user-read-currently-playing user-read-playback-state user-read-recently-played user-top-read", false)]
    public async Task Status_Connected_ReportsWhetherReconnectIsNeeded(string? scope, bool needsReconnect)
    {
        var status = await Connected(scope).GetFromJsonAsync<StatusResponse>("/spotify/status", HearthJson.SnakeCaseLower);

        Assert.True(status!.Authenticated);
        Assert.Equal(needsReconnect, status.NeedsReconnect);
    }

    [Fact]
    public async Task Status_NotConnected_NeedsNoReconnect()
    {
        var status = await _factory.CreateClient().GetFromJsonAsync<StatusResponse>("/spotify/status", HearthJson.SnakeCaseLower);

        Assert.False(status!.Authenticated);
        Assert.False(status.NeedsReconnect);
    }
}
