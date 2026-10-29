using System.Net;
using Data.Extensions;
using ServiceDefaults;

namespace Sports.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForSports(this IServiceCollection services)
    {
        services.AddSqliteDatabase("sports", "sports.db");
        services.AddSqliteDistributedCache("sports");
        services.AddHearthCache();
        // ESPN gzips some responses (the scoreboard) whether or not they were asked for
        services.AddHttpClient<EspnFetcher>()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All });
        services.AddSingleton<FollowedTeamStore>();
        services.AddSingleton<SportsService>();

        services.AddHearthWebDefaults();
        services.AddHealthChecks().AddDatabase("sports");
    }
}
