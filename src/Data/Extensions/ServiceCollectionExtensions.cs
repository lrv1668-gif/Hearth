using Data.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace Data.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqliteDatabase(this IServiceCollection services, string key, string defaultDbFileName)
    {
        var dbPath = Environment.GetEnvironmentVariable("DB_PATH") ?? defaultDbFileName;
        services.AddKeyedSingleton<IDatabase>(key, (_, _) => new Database(dbPath));

        return services;
    }

    /// <summary>
    /// Registers <see cref="SqliteDistributedCache"/> as the <see cref="IDistributedCache"/>, stored in the
    /// database registered under <paramref name="key"/> by <see cref="AddSqliteDatabase"/>.
    /// </summary>
    public static IServiceCollection AddSqliteDistributedCache(this IServiceCollection services, string key)
    {
        services.AddSingleton<IDistributedCache>(sp => new SqliteDistributedCache(
            sp.GetRequiredKeyedService<IDatabase>(key),
            sp.GetService<TimeProvider>()));

        return services;
    }
}
