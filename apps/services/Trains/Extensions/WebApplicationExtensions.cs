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
        FactorySoftTimeout = HearthCache.SoftTimeout,
        EagerRefreshThreshold = HearthCache.EagerRefresh,
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

        // The frame's Scheduled Departures: timetabled times to the end of the next hour,
        // picked once per clock hour so the panel redraws for them at most hourly.
        app.MapGet("/trains/timetable", async (
            IFusionCache cache, TrainsFetcher fetcher, IConfiguration config, TimeProvider clock, string[]? stop, CancellationToken ct) =>
        {
            if (stop is null || stop.Length == 0)
                return Results.Ok(Array.Empty<StopTimetable>());

            if (config.RequireOrFail(
                    app.Logger,
                    _ => Results.Json(new { error = "trains not configured" }, statusCode: 503),
                    "TRANSITLAND_API_KEY") is { } configError)
            {
                return configError;
            }

            var apiKey = config["TRANSITLAND_API_KEY"]!;
            var now = clock.GetUtcNow();
            var hour = Timetable.HourOf(now);
            var options = new FusionCacheEntryOptions
            {
                // Until the next hour's edition; fail-safe keeps an old one only within its own window
                Duration = hour.AddHours(1) - now,
                IsFailSafeEnabled = true,
                FailSafeMaxDuration = TimeSpan.FromHours(Timetable.Hours),
                FactorySoftTimeout = HearthCache.SoftTimeout,
            };

            var results = new List<StopTimetable>();
            foreach (var stopKey in stop)
            {
                try
                {
                    results.Add(await cache.GetOrSetAsync<StopTimetable>(
                        $"timetable:{stopKey}:{hour:yyyyMMddHH}",
                        async (_, token) => Timetable.Build(
                            await fetcher.FetchAsync(apiKey, stopKey, token, Timetable.NextSeconds(now), Timetable.FetchLimit), now),
                        options, ct));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    app.Logger.LogError(ex, "Failed to fetch the timetable for stop {StopKey}", stopKey);
                }
            }

            return Results.Ok(results);
        });
    }
}
