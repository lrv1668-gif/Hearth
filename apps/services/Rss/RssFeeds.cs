using System.Net;
using Rss.Records;
using ServiceDefaults;
using ZiggyCreatures.Caching.Fusion;

namespace Rss;

/// <summary>
/// Feeds through FusionCache: each one is fetched at most every <see cref="Duration"/>, by
/// one caller at a time however many screens ask, and a failed refresh keeps serving the
/// last good copy (retrying every <see cref="RetryAfter"/>) instead of hitting the feed
/// on every request.
/// </summary>
public sealed class RssFeeds(RssFetcher fetcher, IFusionCache cache, ILogger<RssFeeds> logger)
{
    internal static readonly TimeSpan Duration = TimeSpan.FromMinutes(30);
    internal static readonly TimeSpan RetryAfter = TimeSpan.FromMinutes(5);

    private static readonly FusionCacheEntryOptions CacheOptions = new()
    {
        Duration = Duration,
        IsFailSafeEnabled = true,
        FailSafeMaxDuration = TimeSpan.FromDays(1),
        FailSafeThrottleDuration = RetryAfter,
        FactorySoftTimeout = HearthCache.SoftTimeout,
        EagerRefreshThreshold = HearthCache.EagerRefresh,
    };

    /// <summary>The feed, or null when it has never been fetched successfully (logged).</summary>
    public async Task<Feed?> GetAsync(string url, IPAddress pinnedAddress, CancellationToken ct = default)
    {
        try
        {
            return await cache.GetOrSetAsync<Feed>(
                $"rss:{url}", (_, token) => fetcher.FetchAsync(url, pinnedAddress, token), CacheOptions, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            logger.LogError(ex, "Failed to fetch RSS feed: {Url}", url);
            return null;
        }
    }
}
