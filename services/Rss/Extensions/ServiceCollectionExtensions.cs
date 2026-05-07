using System.Text.Json;
using Data;
using Data.Abstractions;

namespace Rss.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForRss(this IServiceCollection services)
    {
        var dbPath = Environment.GetEnvironmentVariable("DB_PATH") ?? "rss.db";

        services.AddKeyedSingleton<IDatabase>("rss", (_, _) => new Database(dbPath));
        services.AddSingleton<RssStore>();
        services.AddHttpClient<RssFetcher>();

        services.AddCors(opts =>
            opts.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
        services.ConfigureHttpJsonOptions(opts =>
            opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);
    }
}
