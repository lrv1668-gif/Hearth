using System.Collections.Concurrent;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Events;

namespace ServiceDefaults;

/// <summary>
/// Which upstream fetches are failing, from FusionCache's factory events: a key's refresh
/// failed and hasn't succeeded since. The widgets keep showing the last good data
/// (fail-safe), so without this a dead API key or an outage is invisible until the cache
/// runs dry. Registered by <see cref="HearthCache.AddHearthCache"/>.
/// </summary>
public sealed class UpstreamFailures : IHealthCheck
{
    /// <summary>A failure older than this with no retry since is a key nothing asks for any more.</summary>
    public static readonly TimeSpan Window = TimeSpan.FromHours(1);

    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastFailure = new();
    private readonly TimeProvider _time;

    public UpstreamFailures(IFusionCache cache, TimeProvider? time = null)
    {
        _time = time ?? TimeProvider.System;
        cache.Events.FactoryError += OnFailure;
        cache.Events.BackgroundFactoryError += OnFailure;
        cache.Events.FactorySuccess += OnSuccess;
        cache.Events.BackgroundFactorySuccess += OnSuccess;
    }

    private void OnFailure(object? sender, FusionCacheEntryEventArgs e) => _lastFailure[e.Key] = _time.GetUtcNow();

    private void OnSuccess(object? sender, FusionCacheEntryEventArgs e) => _lastFailure.TryRemove(e.Key, out _);

    /// <summary>The failing sources — each key's part before the first ':' (no coordinates or queries).</summary>
    public IReadOnlyList<string> Failing()
    {
        var since = _time.GetUtcNow() - Window;
        return _lastFailure
            .Where(kv => kv.Value >= since)
            .Select(kv => kv.Key.Split(':')[0])
            .Distinct()
            .Order()
            .ToList();
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var failing = Failing();
        return Task.FromResult(failing.Count == 0
            ? HealthCheckResult.Healthy("Fetching normally")
            : HealthCheckResult.Degraded($"Can't refresh {string.Join(", ", failing)} — showing the last saved data"));
    }
}
