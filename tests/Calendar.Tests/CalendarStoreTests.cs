using Data.Abstractions;
using Hearth.TestUtilities;
using Microsoft.Extensions.Time.Testing;

namespace Calendar.Tests;

public sealed class CalendarStoreTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 7, 20, 12, 0, 0, TimeSpan.Zero);
    private const string Range = "2026-07-19T00:00:00.0000000Z/2026-09-18T00:00:00.0000000Z";

    private readonly TempDatabase _db = new();
    private readonly FakeTimeProvider _clock = new(Now);
    private readonly CalendarStore _store;

    public CalendarStoreTests()
    {
        _store = new CalendarStore(_db.Db, TestDataProtection.Provider, _clock);
        _store.Migrate();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void Migrate_RunTwice_KeepsTheToken()
    {
        _store.SaveToken("google", "access-1", "refresh-1", Now.AddHours(1));

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
        _store.SaveToken("google", "access-1", "refresh-1", Now.AddHours(1));

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
        var expiresAt = Now.AddHours(1);

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
        _store.SaveToken("google", "access-1", "refresh-1", Now.AddHours(1));
        _store.SaveToken("google", "access-2", "refresh-2", Now.AddHours(2));

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
    public void SaveThenLoad_RoundTripsItemsCacheWithItsRangeAndTime()
    {
        _store.SaveItemsCache("google", Range, "[{\"kind\":\"event\"}]");

        Assert.Equal(("[{\"kind\":\"event\"}]", Range, Now), _store.LoadItemsCache("google"));
    }

    [Fact]
    public void SaveItemsCache_CalledTwice_ReplacesItemsAndCachedAt()
    {
        _store.SaveItemsCache("google", Range, "[]");
        _clock.Advance(TimeSpan.FromMinutes(10));

        _store.SaveItemsCache("google", Range, "[{\"kind\":\"task\"}]");

        Assert.Equal(("[{\"kind\":\"task\"}]", Range, Now.AddMinutes(10)), _store.LoadItemsCache("google"));
    }

    [Fact]
    public void Migrate_CacheRowFromBeforeRanges_ReadsWithNoRange()
    {
        using var legacy = new TempDatabase();
        legacy.Db.NonQuery("CREATE TABLE calendar_items_cache (provider TEXT PRIMARY KEY, items_json TEXT NOT NULL, cached_at TEXT NOT NULL)");
        legacy.Db.NonQuery($"INSERT INTO calendar_items_cache VALUES ('google', '[]', '{Now:o}')");
        var store = new CalendarStore(legacy.Db, TestDataProtection.Provider, _clock);

        store.Migrate();

        Assert.Equal(("[]", (string?)null, Now), store.LoadItemsCache("google"));
    }

    [Fact]
    public void InvalidateItemsCache_RemovesExistingRow()
    {
        _store.SaveItemsCache("google", Range, "[]");

        _store.InvalidateItemsCache("google");

        Assert.Null(_store.LoadItemsCache("google"));
    }

    [Fact]
    public void Clear_RemovesTokenAndCache()
    {
        _store.SaveToken("google", "access-1", "refresh-1", Now.AddHours(1));
        _store.SaveItemsCache("google", Range, "[]");

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
            """, cmd => cmd.AddParam("$expires_at", Now.ToString("o")));

        Assert.Null(_store.LoadToken("google"));
    }

    [Fact]
    public void TokenAndCache_AreIsolatedPerProvider()
    {
        _store.SaveToken("google", "google-access", "google-refresh", Now.AddHours(1));
        _store.SaveItemsCache("google", Range, "[\"google-items\"]");

        Assert.False(_store.HasToken("outlook"));
        Assert.Null(_store.LoadToken("outlook"));
        Assert.Null(_store.LoadItemsCache("outlook"));

        _store.Clear("outlook");

        Assert.True(_store.HasToken("google"));
        Assert.NotNull(_store.LoadItemsCache("google"));
    }
}
