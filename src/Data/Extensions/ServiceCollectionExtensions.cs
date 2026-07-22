using Data.Abstractions;
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
}
