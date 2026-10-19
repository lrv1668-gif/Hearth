using Data.Abstractions;
using ServiceDefaults;
using Weather;
using Weather.Records;
using ZiggyCreatures.Caching.Fusion;

namespace Weather.Extensions;

public static class WebApplicationExtensions
{
    private const string CacheKey = "weather";

    private static readonly FusionCacheEntryOptions CacheOptions = new()
    {
        Duration = TimeSpan.FromMinutes(30),
        IsFailSafeEnabled = true,
        FailSafeMaxDuration = TimeSpan.FromHours(6),
    };

    public static void InitializeWebAppForWeather(this WebApplication app)
    {
        // Pre-FusionCache cache table — its contents are superseded by the distributed_cache table.
        app.Services.GetRequiredKeyedService<IDatabase>("weather").NonQuery("DROP TABLE IF EXISTS weather_cache");
        app.AddWeatherEndpoints();
    }

    private static void AddWeatherEndpoints(this WebApplication app)
    {
        app.MapGet("/weather/current", async (IFusionCache cache, WeatherFetcher fetcher, IConfiguration config, CancellationToken ct) =>
        {
            var (snapshot, error) = await GetWeatherAsync(app, cache, fetcher, config, ct);
            return error ?? Results.Ok(snapshot!.Current);
        });

        app.MapGet("/weather/forecast", async (IFusionCache cache, WeatherFetcher fetcher, IConfiguration config, CancellationToken ct) =>
        {
            var (snapshot, error) = await GetWeatherAsync(app, cache, fetcher, config, ct);
            return error ?? Results.Ok(snapshot!.Forecast);
        });
    }

    private static async Task<(WeatherSnapshot? Snapshot, IResult? Error)> GetWeatherAsync(
        WebApplication app, IFusionCache cache, WeatherFetcher fetcher, IConfiguration config, CancellationToken ct)
    {
        if (config.RequireOrFail(
                app.Logger,
                _ => Results.Json(new { error = "location not configured" }, statusCode: 503),
                "LATITUDE", "LONGITUDE") is { } configError)
        {
            return (null, configError);
        }

        try
        {
            var lat = config.GetValue<double>("LATITUDE");
            var lon = config.GetValue<double>("LONGITUDE");

            // Fail-safe serves the last good snapshot when Open-Meteo is down; this only throws when nothing was ever cached.
            var snapshot = await cache.GetOrSetAsync<WeatherSnapshot>(
                CacheKey, (_, token) => fetcher.FetchAsync(lat, lon, token), CacheOptions, ct);
            return (snapshot, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            app.Logger.LogError(ex, "Failed to fetch weather");
            return (null, Results.Json(new { error = "weather fetch failed" }, statusCode: 502));
        }
    }
}
