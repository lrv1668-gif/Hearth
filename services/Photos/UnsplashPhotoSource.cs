using Photos.Records;

namespace Photos;

public sealed class UnsplashPhotoSource(
    UnsplashCache cache,
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

        if (cache.TryGet(query, orientation, out var cached))
            return cached[Random.Shared.Next(cached.Count)];

        try
        {
            var photos = await fetcher.FetchAsync(query, orientation, key);
            if (photos.Count == 0) return null;
            cache.Set(photos, query, orientation);
            return photos[Random.Shared.Next(photos.Count)];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch photos from Unsplash");
            return null;
        }
    }
}
