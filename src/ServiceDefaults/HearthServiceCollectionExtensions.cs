using Microsoft.Extensions.DependencyInjection;

namespace ServiceDefaults;

public static class HearthServiceCollectionExtensions
{
    public static IServiceCollection AddHearthWebDefaults(this IServiceCollection services)
    {
        services.AddCors(opts =>
            opts.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
        services.ConfigureHttpJsonOptions(opts =>
            opts.SerializerOptions.PropertyNamingPolicy = HearthJson.SnakeCaseLower.PropertyNamingPolicy);

        return services;
    }
}
