using Data.Abstractions;
using Hearth.TestUtilities;

namespace Calendar.Tests;

public sealed class CalendarStoreTests : IDisposable
{
    private readonly TempDatabase _db = new();
    private readonly CalendarStore _store;

    public CalendarStoreTests()
    {
        _store = new CalendarStore(_db.Db, TestDataProtection.Provider);
        _store.Migrate();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void Migrate_RunTwice_KeepsTheToken()
    {
        _store.SaveToken("google", "access-1", "refresh-1", DateTimeOffset.UtcNow.AddHours(1));

        _store.Migrate();

        Assert.True(_store.HasToken("google"));
    }

    [Fact]
    public void HasToken_NoRow_ReturnsFalse()
    {
        Assert.False(_store.HasToken("google"));
    }

    [Fact]
    public void HasToken_RowExists_ReturnsTrue()
    {
        _store.SaveToken("google", "access-1", "refresh-1", DateTimeOffset.UtcNow.AddHours(1));

        Assert.True(_store.HasToken("google"));
    }

    [Fact]
    public void LoadToken_NoRow_ReturnsNull()
    {
        Assert.Null(_store.LoadToken("google"));
    }

    [Fact]
    public void SaveThenLoad_RoundTripsToken()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        _store.SaveToken("google", "access-1", "refresh-1", expiresAt);

        var token = _store.LoadToken("google");
        Assert.NotNull(token);
        Assert.Equal("access-1", token!.AccessToken);
        Assert.Equal("refresh-1", token.RefreshToken);
        Assert.Equal(expiresAt, token.ExpiresAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SaveToken_CalledTwice_ReplacesRow()
    {
        _store.SaveToken("google", "access-1", "refresh-1", DateTimeOffset.UtcNow.AddHours(1));
        _store.SaveToken("google", "access-2", "refresh-2", DateTimeOffset.UtcNow.AddHours(2));

        var token = _store.LoadToken("google");
        Assert.NotNull(token);
        Assert.Equal("access-2", token!.AccessToken);
        Assert.Equal("refresh-2", token.RefreshToken);
    }

    [Fact]
    public void LoadItemsCache_NoRow_ReturnsNull()
    {
        Assert.Null(_store.LoadItemsCache("google"));
    }

    [Fact]
    public void SaveThenLoad_RoundTripsItemsCache()
    {
        var before = DateTimeOffset.UtcNow;

        _store.SaveItemsCache("google", "[{\"kind\":\"event\"}]");

        var cache = _store.LoadItemsCache("google");
        Assert.NotNull(cache);
        Assert.Equal("[{\"kind\":\"event\"}]", cache!.Value.Json);
        Assert.True(cache.Value.CachedAt >= before);
    }

    [Fact]
    public void SaveItemsCache_CalledTwice_ReplacesItemsAndCachedAt()
    {
        // The store stamps "now" itself, so age the first save to tell the two apart
        var tenMinutesAgo = DateTimeOffset.UtcNow.AddMinutes(-10);
        _store.SaveItemsCache("google", "[]");
        _db.Db.NonQuery("UPDATE calendar_items_cache SET cached_at = $t WHERE provider = 'google'",
            c => c.AddParam("$t", tenMinutesAgo.ToString("o")));

        _store.SaveItemsCache("google", "[{\"kind\":\"task\"}]");

        var second = _store.LoadItemsCache("google")!.Value;
        Assert.True(second.CachedAt > tenMinutesAgo);
        Assert.Equal("[{\"kind\":\"task\"}]", second.Json);
    }

    [Fact]
    public void InvalidateItemsCache_RemovesExistingRow()
    {
        _store.SaveItemsCache("google", "[]");

        _store.InvalidateItemsCache("google");

        Assert.Null(_store.LoadItemsCache("google"));
    }

    [Fact]
    public void Clear_RemovesTokenAndCache()
    {
        _store.SaveToken("google", "access-1", "refresh-1", DateTimeOffset.UtcNow.AddHours(1));
        _store.SaveItemsCache("google", "[]");

        _store.Clear("google");

        Assert.Null(_store.LoadToken("google"));
        Assert.Null(_store.LoadItemsCache("google"));
        Assert.False(_store.HasToken("google"));
    }

    [Fact]
    public void LoadToken_LegacyPlaintextRow_ReturnsNullInsteadOfThrowing()
    {
        // Simulate a row written before token encryption was introduced.
        _db.Db.NonQuery("""
            INSERT OR REPLACE INTO calendar_tokens (provider, access_token, refresh_token, expires_at)
            VALUES ('google', 'plaintext-access', 'plaintext-refresh', $expires_at)
            """, cmd => cmd.AddParam("$expires_at", DateTimeOffset.UtcNow.ToString("o")));

        Assert.Null(_store.LoadToken("google"));
    }

    [Fact]
    public void TokenAndCache_AreIsolatedPerProvider()
    {
        _store.SaveToken("google", "google-access", "google-refresh", DateTimeOffset.UtcNow.AddHours(1));
        _store.SaveItemsCache("google", "[\"google-items\"]");

        Assert.False(_store.HasToken("outlook"));
        Assert.Null(_store.LoadToken("outlook"));
        Assert.Null(_store.LoadItemsCache("outlook"));

        _store.Clear("outlook");

        Assert.True(_store.HasToken("google"));
        Assert.NotNull(_store.LoadItemsCache("google"));
    }
}
