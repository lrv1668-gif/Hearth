using Calendar.Tests.Helpers;
using Data.Abstractions;
using Xunit;

namespace Calendar.Tests;

public sealed class CalendarStoreTests
{
    private static CalendarStore Migrated(TempDatabase tmp)
    {
        var store = new CalendarStore(tmp.Db, TestDataProtection.Provider);
        store.Migrate();
        return store;
    }

    [Fact]
    public void Migrate_CalledTwice_DoesNotThrow()
    {
        using var tmp = new TempDatabase();
        var store = new CalendarStore(tmp.Db, TestDataProtection.Provider);

        store.Migrate();
        store.Migrate();
    }

    [Fact]
    public void HasToken_NoRow_ReturnsFalse()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        Assert.False(store.HasToken("google"));
    }

    [Fact]
    public void HasToken_RowExists_ReturnsTrue()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);
        store.SaveToken("google", "access-1", "refresh-1", DateTimeOffset.UtcNow.AddHours(1));

        Assert.True(store.HasToken("google"));
    }

    [Fact]
    public void LoadToken_NoRow_ReturnsNull()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        Assert.Null(store.LoadToken("google"));
    }

    [Fact]
    public void SaveThenLoad_RoundTripsToken()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        store.SaveToken("google", "access-1", "refresh-1", expiresAt);

        var token = store.LoadToken("google");
        Assert.NotNull(token);
        Assert.Equal("access-1", token!.AccessToken);
        Assert.Equal("refresh-1", token.RefreshToken);
        Assert.Equal(expiresAt, token.ExpiresAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SaveToken_CalledTwice_ReplacesRow()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        store.SaveToken("google", "access-1", "refresh-1", DateTimeOffset.UtcNow.AddHours(1));
        store.SaveToken("google", "access-2", "refresh-2", DateTimeOffset.UtcNow.AddHours(2));

        var token = store.LoadToken("google");
        Assert.NotNull(token);
        Assert.Equal("access-2", token!.AccessToken);
        Assert.Equal("refresh-2", token.RefreshToken);
    }

    [Fact]
    public void LoadItemsCache_NoRow_ReturnsNull()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        Assert.Null(store.LoadItemsCache("google"));
    }

    [Fact]
    public void SaveThenLoad_RoundTripsItemsCache()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);
        var before = DateTimeOffset.UtcNow;

        store.SaveItemsCache("google", "[{\"kind\":\"event\"}]");

        var cache = store.LoadItemsCache("google");
        Assert.NotNull(cache);
        Assert.Equal("[{\"kind\":\"event\"}]", cache!.Value.Json);
        Assert.True(cache.Value.CachedAt >= before);
    }

    [Fact]
    public void SaveItemsCache_CalledTwice_UpdatesCachedAt()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        store.SaveItemsCache("google", "[]");
        var first = store.LoadItemsCache("google");

        // Force a distinguishable timestamp by writing an explicit earlier cached_at,
        // then re-saving through the store (which always stamps "now" server-side).
        tmp.Db.NonQuery(
            "UPDATE calendar_items_cache SET cached_at = $t WHERE provider = 'google'",
            c => c.AddParam("$t", DateTimeOffset.UtcNow.AddMinutes(-10).ToString("o")));
        var backdated = store.LoadItemsCache("google");

        store.SaveItemsCache("google", "[{\"kind\":\"task\"}]");
        var second = store.LoadItemsCache("google");

        Assert.NotNull(first);
        Assert.NotNull(backdated);
        Assert.NotNull(second);
        Assert.True(backdated!.Value.CachedAt < first!.Value.CachedAt);
        Assert.True(second!.Value.CachedAt > backdated.Value.CachedAt);
        Assert.Equal("[{\"kind\":\"task\"}]", second.Value.Json);
    }

    [Fact]
    public void InvalidateItemsCache_NoRow_DoesNotThrow()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        store.InvalidateItemsCache("google");
    }

    [Fact]
    public void InvalidateItemsCache_RemovesExistingRow()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);
        store.SaveItemsCache("google", "[]");

        store.InvalidateItemsCache("google");

        Assert.Null(store.LoadItemsCache("google"));
    }

    [Fact]
    public void Clear_RemovesTokenAndCache()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);
        store.SaveToken("google", "access-1", "refresh-1", DateTimeOffset.UtcNow.AddHours(1));
        store.SaveItemsCache("google", "[]");

        store.Clear("google");

        Assert.Null(store.LoadToken("google"));
        Assert.Null(store.LoadItemsCache("google"));
        Assert.False(store.HasToken("google"));
    }

    [Fact]
    public void Clear_NothingStored_DoesNotThrow()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        store.Clear("google");
    }

    [Fact]
    public void LoadToken_LegacyPlaintextRow_ReturnsNullInsteadOfThrowing()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        // Simulate a row written before token encryption was introduced.
        tmp.Db.NonQuery("""
            INSERT OR REPLACE INTO calendar_tokens (provider, access_token, refresh_token, expires_at)
            VALUES ('google', 'plaintext-access', 'plaintext-refresh', $expires_at)
            """, cmd => cmd.AddParam("$expires_at", DateTimeOffset.UtcNow.ToString("o")));

        Assert.Null(store.LoadToken("google"));
    }

    [Fact]
    public void TokenAndCache_AreIsolatedPerProvider()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);
        store.SaveToken("google", "google-access", "google-refresh", DateTimeOffset.UtcNow.AddHours(1));
        store.SaveItemsCache("google", "[\"google-items\"]");

        Assert.False(store.HasToken("outlook"));
        Assert.Null(store.LoadToken("outlook"));
        Assert.Null(store.LoadItemsCache("outlook"));

        store.Clear("outlook");

        Assert.True(store.HasToken("google"));
        Assert.NotNull(store.LoadItemsCache("google"));
    }
}
