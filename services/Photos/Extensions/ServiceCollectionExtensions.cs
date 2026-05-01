using System.Text.Json;
using Data;
using Data.Abstractions;

namespace Photos.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForPhotos(this IServiceCollection services)
    {
        var dbPath = Environment.GetEnvironmentVariable("DB_PATH") ?? "photos.db";

        services.AddKeyedSingleton<IDatabase>("photos", (_, _) => new Database(dbPath));
        services.AddSingleton<PhotoStore>();
        services.AddHttpClient<PhotoFetcher>();

        services.AddCors(opts =>
            opts.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
        services.ConfigureHttpJsonOptions(opts =>
            opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);
    }
}
