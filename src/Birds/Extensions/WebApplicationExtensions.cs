using System.Globalization;
using Data.Abstractions;
using ServiceDefaults;
using Birds;
using Birds.Records;
using ZiggyCreatures.Caching.Fusion;

namespace Birds.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>Per place and radius, so moving the home location doesn't serve the old place's birds.</summary>
    internal static string CacheKey(double lat, double lon, int radiusKm) =>
        string.Create(CultureInfo.InvariantCulture, $"birds:{lat:F3},{lon:F3},{radiusKm}");

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
        app.MapGet("/birds/recent", async (IFusionCache cache, BirdsFetcher fetcher, HomeLocationClient home, IConfiguration config,
            CancellationToken ct) =>
        {
            if (config.RequireOrFail(
                    app.Logger,
                    _ => Results.Json(new { error = "birds not configured" }, statusCode: 503),
                    "EBIRD_API_KEY") is { } configError)
            {
                return configError;
            }
            if ((await home.GetAsync(ct)).Coordinates is not (var lat, var lon))
            {
                app.Logger.LogError("No home location. Set one in Settings → Home, or LATITUDE and LONGITUDE in the .env file.");
                return Results.Json(new { error = "birds not configured" }, statusCode: 503);
            }

            // RequireOrFail above guarantees this is present.
            var apiKey = config["EBIRD_API_KEY"]!;

            try
            {
                var radiusKm = config.GetValue("BIRDS_RADIUS_KM", 15);

                // Fail-safe serves the last good sightings when eBird is down; this only throws when nothing was ever cached.
                var sightings = await cache.GetOrSetAsync<List<BirdSighting>>(
                    CacheKey(lat, lon, radiusKm), (_, token) => fetcher.FetchAsync(apiKey, lat, lon, radiusKm, token), CacheOptions, ct);
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
