using System.Text.Json;
using Weather;
using Weather.Records;

namespace weather.Extensions;

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
            var lat = config["LATITUDE"];
            var lon = config["LONGITUDE"];
            if (string.IsNullOrEmpty(lat) || string.IsNullOrEmpty(lon))
                return Results.Json(new { error = "location not configured" }, statusCode: 503);

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
                return Results.Json(new { error = "location not configured" }, statusCode: 503);

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
