using System.Text.Json;
using Data;
using Data.Abstractions;
using Weather;

namespace Weather.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForWeather(this IServiceCollection services)
    {
        var dbPath = Environment.GetEnvironmentVariable("DB_PATH") ?? "weather.db";

        services.AddKeyedSingleton<IDatabase>("weather", (_, _) => new Database(dbPath));
        services.AddSingleton<WeatherStore>();
        services.AddHttpClient<WeatherFetcher>();

        services.AddCors(opts =>
            opts.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
        services.ConfigureHttpJsonOptions(opts =>
            opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);
    }
}
