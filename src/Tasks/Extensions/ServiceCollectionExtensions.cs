using Data.Extensions;
using ServiceDefaults;
using Tasks;

namespace Tasks.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForTasks(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSqliteDatabase("tasks", "tasks.db");
        serviceCollection.AddSingleton<TaskStore>();

        serviceCollection.AddHearthWebDefaults();
    }
}