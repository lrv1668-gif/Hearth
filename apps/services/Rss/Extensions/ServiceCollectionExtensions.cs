using Data.Extensions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceDefaults;

namespace Rss.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForRss(this IServiceCollection services)
    {
        services.AddSqliteDatabase("rss", "rss.db");
        services.AddSqliteDistributedCache("rss");
        services.AddHearthCache();
        services.AddSingleton<FeedUrlValidator>();
        services.AddSingleton<RssFetcher>();
        services.AddSingleton<RssFeeds>();
        services.AddSingleton<Headlines>();
        services.TryAddSingleton(TimeProvider.System);

        services.AddHearthWebDefaults();
        services.AddHealthChecks().AddDatabase("rss");
    }
}
