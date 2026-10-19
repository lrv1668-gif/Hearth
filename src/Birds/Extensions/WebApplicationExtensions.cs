using Data.Abstractions;
using ServiceDefaults;
using Birds;
using Birds.Records;
using ZiggyCreatures.Caching.Fusion;

namespace Birds.Extensions;

public static class WebApplicationExtensions
{
    private const string CacheKey = "birds";

    private static readonly FusionCacheEntryOptions CacheOptions = new()
    {
        Duration = TimeSpan.FromMinutes(60),
        IsFailSafeEnabled = true,
        FailSafeMaxDuration = TimeSpan.FromHours(24),
    };

    public static void InitializeWebAppForBirds(this WebApplication app)
    {
        // Pre-FusionCache cache table — its contents are superseded by the distributed_cache table.
        app.Services.GetRequiredKeyedService<IDatabase>("birds").NonQuery("DROP TABLE IF EXISTS birds_cache");
        app.AddBirdsEndpoints();
    }

    private static void AddBirdsEndpoints(this WebApplication app)
    {
        app.MapGet("/birds/recent", async (IFusionCache cache, BirdsFetcher fetcher, IConfiguration config, CancellationToken ct) =>
        {
            if (config.RequireOrFail(
                    app.Logger,
                    _ => Results.Json(new { error = "birds not configured" }, statusCode: 503),
                    "EBIRD_API_KEY", "LATITUDE", "LONGITUDE") is { } configError)
            {
                return configError;
            }

            // RequireOrFail above guarantees these are present.
            var apiKey = config["EBIRD_API_KEY"]!;

            try
            {
                var lat = config.GetValue<double>("LATITUDE");
                var lon = config.GetValue<double>("LONGITUDE");
                var radiusKm = config.GetValue("BIRDS_RADIUS_KM", 15);

                // Fail-safe serves the last good sightings when eBird is down; this only throws when nothing was ever cached.
                var sightings = await cache.GetOrSetAsync<List<BirdSighting>>(
                    CacheKey, (_, token) => fetcher.FetchAsync(apiKey, lat, lon, radiusKm, token), CacheOptions, ct);
                return Results.Ok(sightings);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                app.Logger.LogError(ex, "Failed to fetch bird sightings from eBird");
                return Results.Json(new { error = "birds fetch failed" }, statusCode: 502);
            }
        });
    }
}
