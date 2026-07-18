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

        var query = ctx.Query ?? "nature";
        var orientation = ctx.Orientation;
        var cacheKey = $"unsplash:{query}:{orientation}";

        if (cache.TryGetValue(cacheKey, out List<PhotoResponse>? photos) && photos!.Count > 0)
            return photos[Random.Shared.Next(photos.Count)];

        try
        {
            photos = await fetcher.FetchAsync(query, orientation, key);
            if (photos.Count == 0) return null;
            cache.Set(cacheKey, photos, TimeSpan.FromHours(24));
            return photos[Random.Shared.Next(photos.Count)];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch photos from Unsplash");
            return null;
        }
    }
}
