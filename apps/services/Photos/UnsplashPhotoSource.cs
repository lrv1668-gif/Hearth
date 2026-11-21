using Photos.Records;
using ServiceDefaults;
using ZiggyCreatures.Caching.Fusion;

namespace Photos;

public sealed class UnsplashPhotoSource(
    IFusionCache cache,
    PhotoFetcher fetcher,
    HomeLocationClient home,
    IConfiguration config,
    ILogger<UnsplashPhotoSource> logger) : IPhotoSource
{
    public string Key => "unsplash";

    // A day per batch; fail-safe keeps the last batch for a week while Unsplash is down,
    // retrying at most every 5 minutes (the free tier allows 50 requests an hour)
    private static readonly FusionCacheEntryOptions CacheOptions = new()
    {
        Duration = TimeSpan.FromHours(24),
        IsFailSafeEnabled = true,
        FailSafeMaxDuration = TimeSpan.FromDays(7),
        FailSafeThrottleDuration = TimeSpan.FromMinutes(5),
        FactorySoftTimeout = HearthCache.SoftTimeout,
        EagerRefreshThreshold = HearthCache.EagerRefresh,
    };

    public async Task<PhotoResponse?> GetRandomAsync(PhotoSourceContext ctx, CancellationToken ct = default)
    {
        var key = config["UNSPLASH_ACCESS_KEY"];
        if (string.IsNullOrEmpty(key))
        {
            logger.LogError("UNSPLASH_ACCESS_KEY must be set. Update the .env file to add your Unsplash API key.");
            return null;
        }

        // Southern hemisphere flips the season mapping; without a home location, northern
        var isNorthern = (await home.GetAsync(ct)).Latitude is not < 0;
        var query = SeasonalQuery.Expand(ctx.Query ?? "nature", DateOnly.FromDateTime(DateTime.UtcNow), isNorthern);
        var orientation = ctx.Orientation;

        try
        {
            var photos = await cache.GetOrSetAsync<List<PhotoResponse>>(
                $"unsplash:{query}:{orientation}",
                async (_, token) => await fetcher.FetchAsync(query, orientation, key, token) is { Count: > 0 } batch
                    ? batch
                    // Thrown rather than cached, so an empty answer never sits there for a day
                    : throw new InvalidOperationException($"Unsplash returned no photos for \"{query}\""),
                CacheOptions,
                ct);
            return Pick(photos, ctx.Exclude ?? [], Random.Shared);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to fetch photos from Unsplash");
            return null;
        }
    }

    /// <summary>
    /// One of <paramref name="photos"/> at random, other than <paramref name="exclude"/> — or, when
    /// that leaves none, any but the most recently shown (its last id), so a new pass doesn't repeat it.
    /// </summary>
    public static PhotoResponse Pick(IReadOnlyList<PhotoResponse> photos, IReadOnlyList<string> exclude, Random random)
    {
        var fresh = photos.Where(p => !exclude.Contains(p.Id)).ToList();
        if (fresh.Count == 0 && exclude.Count > 0) fresh = photos.Where(p => p.Id != exclude[^1]).ToList();
        if (fresh.Count == 0) fresh = [.. photos];
        return fresh[random.Next(fresh.Count)];
    }
}
