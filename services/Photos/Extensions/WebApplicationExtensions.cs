using System.Text.Json;
using Photos;
using Photos.Records;

namespace Photos.Extensions;

public static class WebApplicationExtensions
{
    public static void InitializeWebAppForPhotos(this WebApplication app)
    {
        app.Services.GetRequiredService<PhotoStore>().Migrate();
        app.AddPhotoEndpoints();
    }

    private static void AddPhotoEndpoints(this WebApplication app)
    {
        app.MapGet("/photos/random", async (
            HttpContext ctx,
            PhotoStore store,
            PhotoFetcher fetcher,
            IConfiguration config) =>
        {
            var query = ctx.Request.Query["query"].FirstOrDefault() ?? "nature";

            var key = config["UNSPLASH_ACCESS_KEY"];
            if (string.IsNullOrEmpty(key))
            {
                app.Logger.LogError("UNSPLASH_ACCESS_KEY must be set. Update the .env file to add your Unsplash API key.");
                return Results.Json(new { error = "API key not configured" }, statusCode: 503);
            }

            var cache = store.Load();
            List<PhotoResponse>? photos = null;

            if (cache is not null && !PhotoStore.IsStale(cache) && PhotoStore.IsQueryMatch(cache, query))
            {
                photos = JsonSerializer.Deserialize<List<PhotoResponse>>(
                    cache.PhotosJson,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
            }

            if (photos is null || photos.Count == 0)
            {
                try
                {
                    photos = await fetcher.FetchAsync(query, key);
                    if (photos.Count == 0)
                        return Results.Json(new { error = "no photos returned" }, statusCode: 502);

                    store.Save(
                        JsonSerializer.Serialize(photos, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }),
                        query);
                }
                catch (Exception ex)
                {
                    app.Logger.LogError(ex, "Failed to fetch photos from Unsplash");
                    return Results.Json(new { error = "photo fetch failed" }, statusCode: 502);
                }
            }

            var pick = photos[Random.Shared.Next(photos.Count)];
            return Results.Ok(pick);
        });
    }
}
