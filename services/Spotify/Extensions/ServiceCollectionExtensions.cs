using System.Text.Json;
using Data;
using Data.Abstractions;
using Spotify;

namespace spotify.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForSpotify(this IServiceCollection services)
    {
        var dbPath = Environment.GetEnvironmentVariable("DB_PATH") ?? "spotify.db";

        services.AddKeyedSingleton<IDatabase>("spotify", (_, _) => new Database(dbPath));
        services.AddSingleton<SpotifyStore>();
        services.AddSingleton<SpotifyClientService>();

        services.AddCors(opts =>
            opts.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
        services.ConfigureHttpJsonOptions(opts =>
            opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);
    }
}
