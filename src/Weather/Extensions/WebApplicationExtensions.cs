using System.Globalization;
using Data.Abstractions;
using ServiceDefaults;
using Weather;
using Weather.Records;
using ZiggyCreatures.Caching.Fusion;

namespace Weather.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>Per place, so moving the home location doesn't serve the old place's weather.</summary>
    internal static string CacheKey(double lat, double lon) =>
        string.Create(CultureInfo.InvariantCulture, $"weather:{lat:F3},{lon:F3}");

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
        app.MapGet("/weather/current", async (IFusionCache cache, WeatherFetcher fetcher, HomeLocationClient home, CancellationToken ct) =>
        {
            var (snapshot, error) = await GetWeatherAsync(app, cache, fetcher, home, ct);
            return error ?? Results.Ok(snapshot!.Current);
        });

        app.MapGet("/weather/forecast", async (IFusionCache cache, WeatherFetcher fetcher, HomeLocationClient home, CancellationToken ct) =>
        {
            var (snapshot, error) = await GetWeatherAsync(app, cache, fetcher, home, ct);
            return error ?? Results.Ok(snapshot!.Forecast);
        });
    }

    private static async Task<(WeatherSnapshot? Snapshot, IResult? Error)> GetWeatherAsync(
        WebApplication app, IFusionCache cache, WeatherFetcher fetcher, HomeLocationClient home, CancellationToken ct)
    {
        if ((await home.GetAsync(ct)).Coordinates is not (var lat, var lon))
        {
            app.Logger.LogError("No home location. Set one in Settings → Home, or LATITUDE and LONGITUDE in the .env file.");
            return (null, Results.Json(new { error = "location not configured" }, statusCode: 503));
        }

        try
        {
            // Fail-safe serves the last good snapshot when Open-Meteo is down; this only throws when nothing was ever cached.
            var snapshot = await cache.GetOrSetAsync<WeatherSnapshot>(
                CacheKey(lat, lon), (_, token) => fetcher.FetchAsync(lat, lon, token), CacheOptions, ct);
            return (snapshot, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            app.Logger.LogError(ex, "Failed to fetch weather");
            return (null, Results.Json(new { error = "weather fetch failed" }, statusCode: 502));
        }
    }
}
