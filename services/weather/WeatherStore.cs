using System.Data.Common;
using Data.Abstractions;

namespace Weather;

public sealed class WeatherStore([FromKeyedServices("weather")] IDatabase db)
{
    public void Migrate() => db.NonQuery("""
        CREATE TABLE IF NOT EXISTS weather_cache (
            id            INTEGER PRIMARY KEY CHECK (id = 1),
            current_json  TEXT    NOT NULL,
            forecast_json TEXT    NOT NULL,
            fetched_at    TEXT    NOT NULL
        )
        """);

    public WeatherCache? Load() =>
        db.QueryOne(
            "SELECT current_json, forecast_json, fetched_at FROM weather_cache WHERE id = 1",
            Map);

    public void Save(string currentJson, string forecastJson) =>
        db.NonQuery("""
            INSERT OR REPLACE INTO weather_cache (id, current_json, forecast_json, fetched_at)
            VALUES (1, $current, $forecast, $fetched_at)
            """, cmd =>
        {
            cmd.AddParam("$current", currentJson);
            cmd.AddParam("$forecast", forecastJson);
            cmd.AddParam("$fetched_at", DateTime.UtcNow.ToString("o"));
        });

    public static bool IsStale(WeatherCache cache)
    {
        if (!DateTime.TryParse(cache.FetchedAt, out var fetched)) return true;
        return (DateTime.UtcNow - fetched).TotalMinutes > 30;
    }

    private static WeatherCache Map(DbDataReader r) =>
        new(r.GetString(0), r.GetString(1), r.GetString(2));
}

public record WeatherCache(string CurrentJson, string ForecastJson, string FetchedAt);
