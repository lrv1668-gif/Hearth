using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using Photos.Records;

namespace Photos;

public sealed class UnsplashPhotoSource(
    IMemoryCache cache,
    PhotoFetcher fetcher,
    IConfiguration config,
    ILogger<UnsplashPhotoSource> logger) : IPhotoSource
{
    public string Key => "unsplash";

    public async Task<PhotoResponse?> GetRandomAsync(PhotoSourceContext ctx)
    {
        var key = config["UNSPLASH_ACCESS_KEY"];
        if (string.IsNullOrEmpty(key))
        {
            logger.LogError("UNSPLASH_ACCESS_KEY must be set. Update the .env file to add your Unsplash API key.");
            return null;
        }

        // Southern hemisphere flips the season mapping; LATITUDE is optional and defaults to northern.
        var isNorthern = !double.TryParse(config["LATITUDE"], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) || lat >= 0;
        var query = SeasonalQuery.Expand(ctx.Query ?? "nature", DateOnly.FromDateTime(DateTime.UtcNow), isNorthern);
        var orientation = ctx.Orientation;
        var cacheKey = $"unsplash:{query}:{orientation}";

        if (cache.TryGetValue(cacheKey, out List<PhotoResponse>? photos) && photos!.Count > 0)
            return Pick(photos, ctx.Exclude ?? [], Random.Shared);

        try
        {
            photos = await fetcher.FetchAsync(query, orientation, key);
            if (photos.Count == 0) return null;
            cache.Set(cacheKey, photos, TimeSpan.FromHours(24));
            return Pick(photos, ctx.Exclude ?? [], Random.Shared);
        }
        catch (Exception ex)
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
