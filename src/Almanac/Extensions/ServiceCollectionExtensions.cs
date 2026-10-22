using ServiceDefaults;

namespace Almanac.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForAlmanac(this IServiceCollection services)
    {
        services.AddSingleton<AlmanacService>();
        services.AddHearthCache();
        services.AddHearthHomeLocation();

        services.AddHearthWebDefaults();
    }
}
