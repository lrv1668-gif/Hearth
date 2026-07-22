using ServiceDefaults;

namespace Quote.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForQuote(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddHttpClient<QuoteFetcher>();

        services.AddHearthWebDefaults();
    }
}
