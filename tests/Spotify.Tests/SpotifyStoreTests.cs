using Data.Abstractions;
using Hearth.TestUtilities;

namespace Spotify.Tests;

public sealed class SpotifyStoreTests : IDisposable
{
    private readonly TempDatabase _db = new();
    private readonly SpotifyStore _store;

    public SpotifyStoreTests()
    {
        _store = new SpotifyStore(_db.Db, TestDataProtection.Provider);
        _store.Migrate();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void SaveThenLoad_RoundTripsTokens()
    {
        var expiresAt = new DateTime(2026, 6, 28, 12, 0, 0, DateTimeKind.Utc);

        _store.Save("access-1", "refresh-1", expiresAt, null);

        var token = _store.Load();
        Assert.NotNull(token);
        Assert.Equal("access-1", token!.AccessToken);
        Assert.Equal("refresh-1", token.RefreshToken);
        Assert.Equal(expiresAt, token.ExpiresAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SaveThenLoad_WithEveryScope_HasRequiredScopes()
    {
        _store.Save("access-1", "refresh-1", DateTime.UtcNow, string.Join(' ', SpotifyScopes.Required));

        Assert.True(_store.Load()!.HasRequiredScopes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("user-read-currently-playing user-read-playback-state")]
    public void Load_LoginMissingHistoryScopes_DoesNotHaveRequiredScopes(string? scope)
    {
        _store.Save("access-1", "refresh-1", DateTime.UtcNow, scope);

        Assert.False(_store.Load()!.HasRequiredScopes);
    }

    [Fact]
    public void Migrate_RunTwice_KeepsTheScopeColumn()
    {
        _store.Migrate();
        _store.Save("access-1", "refresh-1", DateTime.UtcNow, "user-top-read");

        Assert.Equal("user-top-read", _store.Load()!.Scope);
    }

    [Fact]
    public void Load_BeforeAnySave_ReturnsNull()
    {
        Assert.Null(_store.Load());
    }

    [Fact]
    public void Save_CalledTwice_ReplacesSingleRow()
    {
        _store.Save("access-1", "refresh-1", DateTime.UtcNow, null);
        _store.Save("access-2", "refresh-2", DateTime.UtcNow, null);

        var token = _store.Load();
        Assert.NotNull(token);
        Assert.Equal("access-2", token!.AccessToken);
    }

    [Fact]
    public void Clear_RemovesStoredTokens()
    {
        _store.Save("access-1", "refresh-1", DateTime.UtcNow, null);

        _store.Clear();

        Assert.Null(_store.Load());
    }

    [Fact]
    public void Load_LegacyPlaintextRow_ReturnsNullInsteadOfThrowing()
    {
        // Simulate a row written before token encryption was introduced.
        _db.Db.NonQuery("""
            INSERT OR REPLACE INTO spotify_tokens (id, access_token, refresh_token, expires_at)
            VALUES (1, 'plaintext-access', 'plaintext-refresh', $expires_at)
            """, cmd => cmd.AddParam("$expires_at", DateTime.UtcNow.ToString("o")));

        Assert.Null(_store.Load());
    }
}
