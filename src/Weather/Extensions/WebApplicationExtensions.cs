using System.Text.Json;
using ServiceDefaults;
using Weather;
using Weather.Records;

namespace Weather.Extensions;

public static class WebApplicationExtensions
{
    public static void InitializeWebAppForWeather(this WebApplication app)
    {
        app.Services.GetRequiredService<WeatherStore>().Migrate();
        app.AddWeatherEndpoints();
    }

    private static void AddWeatherEndpoints(this WebApplication app)
    {
        app.MapGet("/weather/current", async (WeatherStore store, WeatherFetcher fetcher, IConfiguration config) =>
        {
            if (config.RequireOrFail(
                    app.Logger,
                    _ => Results.Json(new { error = "location not configured" }, statusCode: 503),
                    "LATITUDE", "LONGITUDE") is { } configError)
            {
                return configError;
            }

            // RequireOrFail above guarantees these are present.
            var lat = config["LATITUDE"]!;
            var lon = config["LONGITUDE"]!;

            var cache = store.Load();
            if (cache is not null && !WeatherStore.IsStale(cache))
            {
                var cached = JsonSerializer.Deserialize<CurrentWeatherResponse>(
                    cache.CurrentJson,
                    HearthJson.SnakeCaseLower);
                return Results.Ok(cached);
            }

            try
            {
                var (current, forecast) = await fetcher.FetchAsync(
                    double.Parse(lat), double.Parse(lon));

                store.Save(
                    JsonSerializer.Serialize(current, HearthJson.SnakeCaseLower),
                    JsonSerializer.Serialize(forecast, HearthJson.SnakeCaseLower));

                return Results.Ok(current);
            }
            catch (Exception ex)
            {
                app.Logger.LogError(ex, "Failed to fetch weather");
                return Results.Json(new { error = "weather fetch failed" }, statusCode: 502);
            }
        });

        app.MapGet("/weather/forecast", async (WeatherStore store, WeatherFetcher fetcher, IConfiguration config) =>
        {
            if (config.RequireOrFail(
                    app.Logger,
                    _ => Results.Json(new { error = "location not configured" }, statusCode: 503),
                    "LATITUDE", "LONGITUDE") is { } configError)
            {
                return configError;
            }

            // RequireOrFail above guarantees these are present.
            var lat = config["LATITUDE"]!;
            var lon = config["LONGITUDE"]!;

            var cache = store.Load();
            if (cache is not null && !WeatherStore.IsStale(cache))
            {
                var cached = JsonSerializer.Deserialize<List<ForecastDayResponse>>(
                    cache.ForecastJson,
                    HearthJson.SnakeCaseLower);
                return Results.Ok(cached);
            }

            try
            {
                var (current, forecast) = await fetcher.FetchAsync(
                    double.Parse(lat), double.Parse(lon));

                store.Save(
                    JsonSerializer.Serialize(current, HearthJson.SnakeCaseLower),
                    JsonSerializer.Serialize(forecast, HearthJson.SnakeCaseLower));

                return Results.Ok(forecast);
            }
            catch (Exception ex)
            {
                app.Logger.LogError(ex, "Failed to fetch weather");
                return Results.Json(new { error = "weather fetch failed" }, statusCode: 502);
            }
        });
    }
}
