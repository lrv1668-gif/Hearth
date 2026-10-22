using Data.Extensions;
using ServiceDefaults;
using Weather;

namespace Weather.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForWeather(this IServiceCollection services)
    {
        services.AddSqliteDatabase("weather", "weather.db");
        services.AddSqliteDistributedCache("weather");
        services.AddHearthCache();
        services.AddHttpClient<WeatherFetcher>();
        services.AddHearthHomeLocation();

        services.AddHearthWebDefaults();
    }
}
