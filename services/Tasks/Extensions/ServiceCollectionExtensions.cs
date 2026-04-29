using System.Text.Json;
using Data;
using Data.Abstractions;
using Tasks;

namespace tasks.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForTasks(this IServiceCollection serviceCollection)
    {
        var tasksDbPath = Environment.GetEnvironmentVariable("DB_PATH") ?? "tasks.db";

        serviceCollection.AddKeyedSingleton<IDatabase>("tasks", (_, _) => new Database(tasksDbPath));
        serviceCollection.AddSingleton<TaskStore>();

        serviceCollection.AddCors(opts =>
            opts.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
        serviceCollection.ConfigureHttpJsonOptions(opts =>
            opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);

    }
}