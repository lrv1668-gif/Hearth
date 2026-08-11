using Data.Extensions;
using ServiceDefaults;

namespace Trains.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForTrains(this IServiceCollection services)
    {
        services.AddSqliteDatabase("trains", "trains.db");
        services.AddSingleton<TrainsStore>();
        services.AddHttpClient<TrainsFetcher>();

        services.AddHearthWebDefaults();
    }
}
