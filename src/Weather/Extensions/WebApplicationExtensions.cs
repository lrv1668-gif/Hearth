using System.Text.Json;
using Weather;
using Weather.Records;

namespace Weather.Extensions;

public static class WebApplicationExtensions
{
    public static void InitializeWebAppForWeather(this WebApplication app)
    {
        app.Services.GetRequiredService<WeatherStore>().Migrate();

        var health = GetHealth(app.Configuration);
        if (health.Configured)
            app.Logger.LogInformation("Weather configured — LATITUDE, LONGITUDE set");
        else
            app.Logger.LogWarning(
                "Weather not configured — missing {Missing}; /weather endpoints will return 503 until set in src/Weather/.env",
                string.Join(", ", health.Missing));

        app.AddWeatherEndpoints();
    }

    private static HealthResponse GetHealth(IConfiguration config) =>
        Health.Evaluate(("LATITUDE", config["LATITUDE"]), ("LONGITUDE", config["LONGITUDE"]));

    private static void AddWeatherEndpoints(this WebApplication app)
    {
        app.MapGet("/weather/health", (IConfiguration config) => Results.Ok(GetHealth(config)));

        app.MapGet("/weather/current", async (WeatherStore store, WeatherFetcher fetcher, IConfiguration config) =>
        {
            var lat = config["LATITUDE"];
            var lon = config["LONGITUDE"];
            if (string.IsNullOrEmpty(lat) || string.IsNullOrEmpty(lon))
            {
                app.Logger.LogError("LATITUDE and LONGITUDE must be set. Update the .env file to add your coordinates.");
                return Results.Json(new { error = "location not configured" }, statusCode: 503);
            }

            var cache = store.Load();
            if (cache is not null && !WeatherStore.IsStale(cache))
            {
                var cached = JsonSerializer.Deserialize<CurrentWeatherResponse>(
                    cache.CurrentJson,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
                return Results.Ok(cached);
            }

            try
            {
                var (current, forecast) = await fetcher.FetchAsync(
                    double.Parse(lat), double.Parse(lon));

                store.Save(
                    JsonSerializer.Serialize(current, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }),
                    JsonSerializer.Serialize(forecast, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }));

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
            var lat = config["LATITUDE"];
            var lon = config["LONGITUDE"];
            if (string.IsNullOrEmpty(lat) || string.IsNullOrEmpty(lon))
            {
                app.Logger.LogError("LATITUDE and LONGITUDE must be set. Update the .env file to add your coordinates.");
                return Results.Json(new { error = "location not configured" }, statusCode: 503);
            }

            var cache = store.Load();
            if (cache is not null && !WeatherStore.IsStale(cache))
            {
                var cached = JsonSerializer.Deserialize<List<ForecastDayResponse>>(
                    cache.ForecastJson,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
                return Results.Ok(cached);
            }

            try
            {
                var (current, forecast) = await fetcher.FetchAsync(
                    double.Parse(lat), double.Parse(lon));

                store.Save(
                    JsonSerializer.Serialize(current, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }),
                    JsonSerializer.Serialize(forecast, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }));

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
