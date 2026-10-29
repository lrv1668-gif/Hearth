using Data.Abstractions;
using Microsoft.Extensions.Caching.Distributed;

namespace Data;

/// <summary>
/// <see cref="IDistributedCache"/> over a service's SQLite <see cref="IDatabase"/>. Serves as
/// FusionCache's L2 so cached upstream API responses survive container restarts.
/// </summary>
/// <remarks>
/// Sliding expiration is not supported — <see cref="Refresh"/> is a no-op. FusionCache only ever
/// writes absolute expirations, so this never matters in practice.
/// </remarks>
public sealed class SqliteDistributedCache : IDistributedCache
{
    private readonly IDatabase _db;
    private readonly TimeProvider _time;

    public SqliteDistributedCache(IDatabase db, TimeProvider? time = null)
    {
        _db = db;
        _time = time ?? TimeProvider.System;

        _db.NonQuery("""
            CREATE TABLE IF NOT EXISTS distributed_cache (
                key        TEXT    PRIMARY KEY,
                value      BLOB    NOT NULL,
                expires_at INTEGER NOT NULL
            )
            """);
    }

    public byte[]? Get(string key)
    {
        var entry = _db.QueryOne(
            "SELECT value, expires_at FROM distributed_cache WHERE key = $key",
            r => new Entry(r.Field<byte[]>("value")!, r.Field<long>("expires_at")),
            cmd => cmd.AddParam("$key", key));

        if (entry is null) return null;
        if (entry.ExpiresAt > Now()) return entry.Value;

        Remove(key);
        return null;
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        var now = _time.GetUtcNow();
        var ttl = options.AbsoluteExpirationRelativeToNow ?? options.SlidingExpiration;
        var expiresAt = options.AbsoluteExpiration
            ?? (ttl is { } relative ? now + relative : DateTimeOffset.MaxValue);

        // Opportunistic purge keeps the table from accumulating entries for keys that are never read again
        _db.NonQuery("DELETE FROM distributed_cache WHERE expires_at <= $now",
            cmd => cmd.AddParam("$now", now.ToUnixTimeMilliseconds()));

        _db.NonQuery("""
            INSERT OR REPLACE INTO distributed_cache (key, value, expires_at)
            VALUES ($key, $value, $expires_at)
            """, cmd =>
        {
            cmd.AddParam("$key", key);
            cmd.AddParam("$value", value);
            cmd.AddParam("$expires_at", expiresAt.ToUnixTimeMilliseconds());
        });
    }

    public void Remove(string key) =>
        _db.NonQuery("DELETE FROM distributed_cache WHERE key = $key", cmd => cmd.AddParam("$key", key));

    public void Refresh(string key) { }

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
        Task.FromResult(Get(key));

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        Set(key, value, options);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        Remove(key);
        return Task.CompletedTask;
    }

    public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

    private long Now() => _time.GetUtcNow().ToUnixTimeMilliseconds();

    private sealed record Entry(byte[] Value, long ExpiresAt);
}
