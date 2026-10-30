using Data.Abstractions;
using Hearth.TestUtilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Data.Tests;

public sealed class SqliteDistributedCacheTests : IDisposable
{
    private readonly TempDatabase _tmp = new();
    private readonly FakeTimeProvider _time = new(DateTimeOffset.Parse("2026-10-18T12:00:00Z"));
    private readonly SqliteDistributedCache _cache;

    public SqliteDistributedCacheTests()
    {
        _cache = new SqliteDistributedCache(_tmp.Db, _time);
    }

    public void Dispose() => _tmp.Dispose();

    private static DistributedCacheEntryOptions ExpiresIn(TimeSpan ttl) =>
        new() { AbsoluteExpirationRelativeToNow = ttl };

    [Fact]
    public void Get_AfterSet_ReturnsStoredBytes()
    {
        _cache.Set("k", [1, 2, 3], ExpiresIn(TimeSpan.FromMinutes(5)));

        Assert.Equal([1, 2, 3], _cache.Get("k"));
    }

    [Fact]
    public void Get_UnknownKey_ReturnsNull()
    {
        Assert.Null(_cache.Get("missing"));
    }

    [Fact]
    public void Get_PastExpiry_ReturnsNull()
    {
        _cache.Set("k", [1], ExpiresIn(TimeSpan.FromMinutes(5)));

        _time.Advance(TimeSpan.FromMinutes(6));

        Assert.Null(_cache.Get("k"));
    }

    [Fact]
    public void Get_AbsoluteExpiration_HonoredOverRelative()
    {
        _cache.Set("k", [1], new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = _time.GetUtcNow().AddMinutes(1),
        });

        _time.Advance(TimeSpan.FromMinutes(2));

        Assert.Null(_cache.Get("k"));
    }

    [Fact]
    public void Set_ExistingKey_Overwrites()
    {
        _cache.Set("k", [1], ExpiresIn(TimeSpan.FromMinutes(5)));
        _cache.Set("k", [2], ExpiresIn(TimeSpan.FromMinutes(5)));

        Assert.Equal([2], _cache.Get("k"));
    }

    [Fact]
    public void Remove_ExistingKey_GetReturnsNull()
    {
        _cache.Set("k", [1], ExpiresIn(TimeSpan.FromMinutes(5)));

        _cache.Remove("k");

        Assert.Null(_cache.Get("k"));
    }

    [Fact]
    public void Get_NewInstanceOverSameDatabase_SeesEarlierWrites()
    {
        _cache.Set("k", [7], ExpiresIn(TimeSpan.FromMinutes(5)));

        var restarted = new SqliteDistributedCache(_tmp.Db, _time);

        Assert.Equal([7], restarted.Get("k"));
    }

    private long RowCount() =>
        _tmp.Db.QueryOne("SELECT COUNT(*) AS n FROM distributed_cache", r => r.Field<long>("n"));

    [Fact]
    public void Set_WithinPurgeInterval_LeavesExpiredRows()
    {
        _cache.Set("old", [1], ExpiresIn(TimeSpan.FromMinutes(1)));
        _time.Advance(TimeSpan.FromMinutes(2));

        _cache.Set("new", [2], ExpiresIn(TimeSpan.FromMinutes(5)));

        Assert.Equal(2, RowCount());
    }

    [Fact]
    public void Set_AfterPurgeInterval_PurgesExpiredRows()
    {
        _cache.Set("old", [1], ExpiresIn(TimeSpan.FromMinutes(1)));
        _time.Advance(SqliteDistributedCache.PurgeInterval);

        _cache.Set("new", [2], ExpiresIn(TimeSpan.FromMinutes(5)));

        Assert.Equal(1, RowCount());
        Assert.Equal([2], _cache.Get("new"));
    }
}
