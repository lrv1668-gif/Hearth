using System.Text.Json;
using Data;
using Data.Abstractions;
using Birds;

namespace Birds.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForBirds(this IServiceCollection services)
    {
        var dbPath = Environment.GetEnvironmentVariable("DB_PATH") ?? "birds.db";

        services.AddKeyedSingleton<IDatabase>("birds", (_, _) => new Database(dbPath));
        services.AddSingleton<BirdsStore>();
        services.AddHttpClient<BirdsFetcher>();

        services.AddCors(opts =>
            opts.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
        services.ConfigureHttpJsonOptions(opts =>
            opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);
    }
}
