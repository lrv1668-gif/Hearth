using System.Text.Json;
using ServiceDefaults;
using Trains.Records;

namespace Trains.Extensions;

public static class WebApplicationExtensions
{
    public static void InitializeWebAppForTrains(this WebApplication app)
    {
        app.Services.GetRequiredService<TrainsStore>().Migrate();
        app.AddTrainsEndpoints();
    }

    private static void AddTrainsEndpoints(this WebApplication app)
    {
        app.MapGet("/trains/departures", async (TrainsStore store, TrainsFetcher fetcher, IConfiguration config, string[]? stop) =>
        {
            if (stop is null || stop.Length == 0)
                return Results.Ok(Array.Empty<StopDepartures>());

            if (config.RequireOrFail(
                    app.Logger,
                    _ => Results.Json(new { error = "trains not configured" }, statusCode: 503),
                    "TRANSITLAND_API_KEY") is { } configError)
            {
                return configError;
            }

            var apiKey = config["TRANSITLAND_API_KEY"]!;

            var results = new List<StopDepartures>();
            foreach (var stopKey in stop)
            {
                if (store.IsStale(stopKey))
                {
                    try
                    {
                        var fetched = await fetcher.FetchAsync(apiKey, stopKey);
                        store.Save(stopKey, JsonSerializer.Serialize(fetched, HearthJson.SnakeCaseLower));
                    }
                    catch (Exception ex)
                    {
                        app.Logger.LogError(ex, "Failed to fetch departures for stop {StopKey}", stopKey);
                    }
                }

                var cachedJson = store.Load(stopKey);
                if (cachedJson is not null)
                {
                    var cached = JsonSerializer.Deserialize<StopDepartures>(cachedJson, HearthJson.SnakeCaseLower);
                    if (cached is not null)
                        results.Add(cached);
                }
            }

            return Results.Ok(results);
        });
    }
}
