using Microsoft.AspNetCore.DataProtection;
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

    /// <summary>
    /// Configures Data Protection with keys persisted next to the service's DB_PATH,
    /// so protected values (e.g. OAuth tokens) survive container redeploys instead of
    /// being invalidated by the default ephemeral in-memory key store.
    /// </summary>
    public static IServiceCollection AddHearthDataProtection(this IServiceCollection services, string appName)
    {
        var dbPath = Environment.GetEnvironmentVariable("DB_PATH") ?? $"{appName}.db";
        var dbDir = Path.GetDirectoryName(Path.GetFullPath(dbPath))!;
        var keysDir = new DirectoryInfo(Path.Combine(dbDir, "keys"));

        services.AddDataProtection()
            .PersistKeysToFileSystem(keysDir)
            .SetApplicationName($"hearth-{appName}");

        return services;
    }
}
