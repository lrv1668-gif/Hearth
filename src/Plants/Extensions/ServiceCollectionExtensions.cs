using Data.Extensions;
using ServiceDefaults;
using Plants;

namespace Plants.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForPlants(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSqliteDatabase("plants", "plants.db");
        serviceCollection.AddSingleton<PlantStore>();

        serviceCollection.AddHearthWebDefaults();
    }
}
