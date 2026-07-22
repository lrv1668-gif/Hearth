using System.Text.Json;
using ServiceDefaults;
using Birds;
using Birds.Records;

namespace Birds.Extensions;

public static class WebApplicationExtensions
{
    public static void InitializeWebAppForBirds(this WebApplication app)
    {
        app.Services.GetRequiredService<BirdsStore>().Migrate();
        app.AddBirdsEndpoints();
    }

    private static void AddBirdsEndpoints(this WebApplication app)
    {
        app.MapGet("/birds/recent", async (BirdsStore store, BirdsFetcher fetcher, IConfiguration config) =>
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
            var lat = config["LATITUDE"]!;
            var lon = config["LONGITUDE"]!;

            var cache = store.Load();
            if (cache is not null && !BirdsStore.IsStale(cache))
            {
                var cached = JsonSerializer.Deserialize<List<BirdSighting>>(
                    cache.SightingsJson,
                    HearthJson.SnakeCaseLower);
                return Results.Ok(cached);
            }

            try
            {
                var radiusKm = int.TryParse(config["BIRDS_RADIUS_KM"], out var r) ? r : 15;
                var sightings = await fetcher.FetchAsync(
                    apiKey, double.Parse(lat), double.Parse(lon), radiusKm);

                store.Save(JsonSerializer.Serialize(
                    sightings,
                    HearthJson.SnakeCaseLower));

                return Results.Ok(sightings);
            }
            catch (Exception ex)
            {
                app.Logger.LogError(ex, "Failed to fetch bird sightings from eBird");
                return Results.Json(new { error = "birds fetch failed" }, statusCode: 502);
            }
        });
    }
}
