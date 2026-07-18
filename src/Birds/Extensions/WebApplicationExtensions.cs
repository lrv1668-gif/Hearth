using System.Text.Json;
using Birds;
using Birds.Records;

namespace Birds.Extensions;

public static class WebApplicationExtensions
{
    public static void InitializeWebAppForBirds(this WebApplication app)
    {
        app.Services.GetRequiredService<BirdsStore>().Migrate();

        var health = GetHealth(app.Configuration);
        if (health.Configured)
            app.Logger.LogInformation("Birds configured — EBIRD_API_KEY, LATITUDE, LONGITUDE set");
        else
            app.Logger.LogWarning(
                "Birds not configured — missing {Missing}; /birds endpoints will return 503 until set in src/Birds/.env",
                string.Join(", ", health.Missing));

        app.AddBirdsEndpoints();
    }

    private static HealthResponse GetHealth(IConfiguration config) =>
        Health.Evaluate(
            ("EBIRD_API_KEY", config["EBIRD_API_KEY"]),
            ("LATITUDE", config["LATITUDE"]),
            ("LONGITUDE", config["LONGITUDE"]));

    private static void AddBirdsEndpoints(this WebApplication app)
    {
        app.MapGet("/birds/health", (IConfiguration config) => Results.Ok(GetHealth(config)));

        app.MapGet("/birds/recent", async (BirdsStore store, BirdsFetcher fetcher, IConfiguration config) =>
        {
            var apiKey = config["EBIRD_API_KEY"];
            var lat = config["LATITUDE"];
            var lon = config["LONGITUDE"];
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(lat) || string.IsNullOrEmpty(lon))
            {
                app.Logger.LogError(
                    "EBIRD_API_KEY, LATITUDE, and LONGITUDE must be set. Update the .env file to add your eBird API key (free at https://ebird.org/api/keygen) and coordinates.");
                return Results.Json(new { error = "birds not configured" }, statusCode: 503);
            }

            var cache = store.Load();
            if (cache is not null && !BirdsStore.IsStale(cache))
            {
                var cached = JsonSerializer.Deserialize<List<BirdSighting>>(
                    cache.SightingsJson,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
                return Results.Ok(cached);
            }

            try
            {
                var radiusKm = int.TryParse(config["BIRDS_RADIUS_KM"], out var r) ? r : 15;
                var sightings = await fetcher.FetchAsync(
                    apiKey, double.Parse(lat), double.Parse(lon), radiusKm);

                store.Save(JsonSerializer.Serialize(
                    sightings,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }));

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
