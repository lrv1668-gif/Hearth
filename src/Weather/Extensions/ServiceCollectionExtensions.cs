using Data.Extensions;
using ServiceDefaults;
using Weather;

namespace Weather.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForWeather(this IServiceCollection services)
    {
        services.AddSqliteDatabase("weather", "weather.db");
        services.AddSingleton<WeatherStore>();
        services.AddHttpClient<WeatherFetcher>();

        services.AddHearthWebDefaults();
    }
}
