using System.Text.Json;

namespace Quote.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForQuote(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddHttpClient<QuoteFetcher>();

        services.AddCors(opts =>
            opts.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
        services.ConfigureHttpJsonOptions(opts =>
            opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);
    }
}
