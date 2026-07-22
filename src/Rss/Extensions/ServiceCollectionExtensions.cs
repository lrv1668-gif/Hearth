using Data.Extensions;
using ServiceDefaults;

namespace Rss.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForRss(this IServiceCollection services)
    {
        services.AddSqliteDatabase("rss", "rss.db");
        services.AddSingleton<RssStore>();
        services.AddSingleton<FeedUrlValidator>();
        services.AddHttpClient<RssFetcher>();

        services.AddHearthWebDefaults();
    }
}
