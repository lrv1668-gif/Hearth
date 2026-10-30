using Hearth.TestUtilities;
using Microsoft.Extensions.Configuration;

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
        store.Save("access-1", "refresh-1", DateTime.UtcNow.AddHours(1), null);
        var clientService = new SpotifyClientService(store, MakeConfig());

        Assert.NotNull(clientService.TryGetClient());
    }

    [Fact]
    public void TryGetClient_TokenPresentButExpired_ReturnsNonNullClient()
    {
        using var tmp = new TempDatabase();
        var store = MakeStore(tmp);
        store.Save("access-1", "refresh-1", DateTime.UtcNow.AddHours(-1), null);
        var clientService = new SpotifyClientService(store, MakeConfig());

        // The client is still constructed (with an expired-marker ExpiresIn) so it refreshes
        // on first use — LastPlayedEndpointTests.Get_TokenRefreshWithoutScope_KeepsTheStoredScope
        // drives that refresh through a stubbed token endpoint.
        Assert.NotNull(clientService.TryGetClient());
    }
}
