using Data.Abstractions;
using ServiceDefaults;
using Trains.Records;
using ZiggyCreatures.Caching.Fusion;

namespace Trains.Extensions;

public static class WebApplicationExtensions
{
    // Departures go stale fast — past the fail-safe window an old board is worse than none.
    private static readonly FusionCacheEntryOptions CacheOptions = new()
    {
        Duration = TimeSpan.FromSeconds(45),
        IsFailSafeEnabled = true,
        FailSafeMaxDuration = TimeSpan.FromMinutes(10),
    };

    public static void InitializeWebAppForTrains(this WebApplication app)
    {
        // Pre-FusionCache cache table — its contents are superseded by the distributed_cache table.
        app.Services.GetRequiredKeyedService<IDatabase>("trains").NonQuery("DROP TABLE IF EXISTS trains_cache");
        app.AddTrainsEndpoints();
        app.MapHearthHealth("trains");
    }

    private static void AddTrainsEndpoints(this WebApplication app)
    {
        app.MapGet("/trains/departures", async (IFusionCache cache, TrainsFetcher fetcher, IConfiguration config, string[]? stop, CancellationToken ct) =>
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

            // A stop that has never fetched successfully is left out rather than failing the whole board.
            var results = new List<StopDepartures>();
            foreach (var stopKey in stop)
            {
                try
                {
                    results.Add(await cache.GetOrSetAsync<StopDepartures>(
                        $"trains:{stopKey}", (_, token) => fetcher.FetchAsync(apiKey, stopKey, token), CacheOptions, ct));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    app.Logger.LogError(ex, "Failed to fetch departures for stop {StopKey}", stopKey);
                }
            }

            return Results.Ok(results);
        });
    }
}
