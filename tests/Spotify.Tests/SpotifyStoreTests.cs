using Data.Abstractions;
using Spotify.Tests.Helpers;
using Xunit;

namespace Spotify.Tests;

public sealed class SpotifyStoreTests
{
    private static SpotifyStore Migrated(TempDatabase tmp)
    {
        var store = new SpotifyStore(tmp.Db, TestDataProtection.Provider);
        store.Migrate();
        return store;
    }

    [Fact]
    public void SaveThenLoad_RoundTripsTokens()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);
        var expiresAt = new DateTime(2026, 6, 28, 12, 0, 0, DateTimeKind.Utc);

        store.Save("access-1", "refresh-1", expiresAt);

        var token = store.Load();
        Assert.NotNull(token);
        Assert.Equal("access-1", token!.AccessToken);
        Assert.Equal("refresh-1", token.RefreshToken);
        Assert.Equal(expiresAt, token.ExpiresAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Load_BeforeAnySave_ReturnsNull()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        Assert.Null(store.Load());
    }

    [Fact]
    public void Save_CalledTwice_ReplacesSingleRow()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        store.Save("access-1", "refresh-1", DateTime.UtcNow);
        store.Save("access-2", "refresh-2", DateTime.UtcNow);

        var token = store.Load();
        Assert.NotNull(token);
        Assert.Equal("access-2", token!.AccessToken);
    }

    [Fact]
    public void Clear_RemovesStoredTokens()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);
        store.Save("access-1", "refresh-1", DateTime.UtcNow);

        store.Clear();

        Assert.Null(store.Load());
    }

    [Fact]
    public void Load_LegacyPlaintextRow_ReturnsNullInsteadOfThrowing()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        // Simulate a row written before token encryption was introduced.
        tmp.Db.NonQuery("""
            INSERT OR REPLACE INTO spotify_tokens (id, access_token, refresh_token, expires_at)
            VALUES (1, 'plaintext-access', 'plaintext-refresh', $expires_at)
            """, cmd => cmd.AddParam("$expires_at", DateTime.UtcNow.ToString("o")));

        Assert.Null(store.Load());
    }
}
