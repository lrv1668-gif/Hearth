using Microsoft.Extensions.Configuration;
using Spotify.Tests.Helpers;
using Xunit;

namespace Spotify.Tests;

public sealed class SpotifyClientServiceTests
{
    private static IConfiguration MakeConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SPOTIFY_CLIENT_ID"] = "test-client-id",
                ["SPOTIFY_CLIENT_SECRET"] = "test-client-secret",
            })
            .Build();

    private static SpotifyStore MakeStore(TempDatabase tmp)
    {
        var store = new SpotifyStore(tmp.Db, TestDataProtection.Provider);
        store.Migrate();
        return store;
    }

    [Fact]
    public void TryGetClient_NoStoredToken_ReturnsNull()
    {
        using var tmp = new TempDatabase();
        var store = MakeStore(tmp);
        var clientService = new SpotifyClientService(store, MakeConfig());

        Assert.Null(clientService.TryGetClient());
    }

    [Fact]
    public void TryGetClient_TokenPresentAndNotExpired_ReturnsNonNullClient()
    {
        using var tmp = new TempDatabase();
        var store = MakeStore(tmp);
        store.Save("access-1", "refresh-1", DateTime.UtcNow.AddHours(1));
        var clientService = new SpotifyClientService(store, MakeConfig());

        Assert.NotNull(clientService.TryGetClient());
    }

    [Fact]
    public void TryGetClient_TokenPresentButExpired_ReturnsNonNullClient()
    {
        using var tmp = new TempDatabase();
        var store = MakeStore(tmp);
        store.Save("access-1", "refresh-1", DateTime.UtcNow.AddHours(-1));
        var clientService = new SpotifyClientService(store, MakeConfig());

        // The client is still constructed (with an expired-marker ExpiresIn) so it can
        // attempt a refresh on first use — the actual refresh call isn't reachable without
        // a live HTTP request to Spotify's token endpoint, so it isn't exercised here.
        Assert.NotNull(clientService.TryGetClient());
    }
}
