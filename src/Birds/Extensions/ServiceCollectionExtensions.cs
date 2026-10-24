using Data.Extensions;
using ServiceDefaults;
using Birds;

namespace Birds.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForBirds(this IServiceCollection services)
    {
        services.AddSqliteDatabase("birds", "birds.db");
        services.AddSqliteDistributedCache("birds");
        services.AddHearthCache();
        services.AddHttpClient<BirdsFetcher>();
        services.AddHearthHomeLocation();

        services.AddHearthWebDefaults();
        services.AddHealthChecks().AddDatabase("birds").AddRequiredConfig("EBIRD_API_KEY").AddHomeLocation();
    }
}
