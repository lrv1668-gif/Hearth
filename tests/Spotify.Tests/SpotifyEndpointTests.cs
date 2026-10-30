using Hearth.TestUtilities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Spotify.Tests;

/// <summary>The Spotify service in memory against a stubbed Spotify API, for the endpoint tests.</summary>
public abstract class SpotifyEndpointTests : IDisposable
{
    protected static readonly string AllScopes = string.Join(' ', SpotifyScopes.Required);

    private readonly TempDatabase _db = new();
    protected readonly FakeHttpMessageHandler _spotify;
    protected readonly WebApplicationFactory<Program> _factory;

    protected SpotifyEndpointTests(string spotifyJson)
    {
        _spotify = new FakeHttpMessageHandler(spotifyJson);
        _factory = HearthApp.Create<Program>("spotify", _db, host =>
        {
            host.UseSetting("SPOTIFY_CLIENT_ID", "test-client-id");
            host.UseSetting("SPOTIFY_CLIENT_SECRET", "test-client-secret");
            host.ConfigureTestServices(services =>
            {
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

    protected SpotifyStore Store => _factory.Services.GetRequiredService<SpotifyStore>();

    /// <summary>A client for the service with a saved, unexpired login granting <paramref name="scope"/>.</summary>
    protected HttpClient Connected(string? scope)
    {
        var client = _factory.CreateClient();
        Store.Save("access", "refresh", DateTime.UtcNow.AddHours(1), scope);
        return client;
    }
}
