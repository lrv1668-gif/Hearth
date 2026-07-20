using System.Text.Json;

namespace Almanac.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForAlmanac(this IServiceCollection services)
    {
        services.AddSingleton<AlmanacService>();

        services.AddCors(opts =>
            opts.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
        services.ConfigureHttpJsonOptions(opts =>
            opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);
    }
}
