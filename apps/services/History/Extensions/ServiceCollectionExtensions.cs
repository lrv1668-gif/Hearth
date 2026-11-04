using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceDefaults;

namespace History.Extensions;

public static class ServiceCollectionExtensions
{
    // Wikimedia asks API clients to name themselves; generic, with no contact details.
    private const string UserAgent = "Hearth/1.0 (self-hosted home dashboard; +https://github.com/lrv1668-gif/Hearth)";

    public static void AddServicesForHistory(this IServiceCollection services)
    {
        services.AddHearthCache();
        services.AddHttpClient<HistoryFetcher>(http => http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent));
        services.AddHearthHomeLocation();
        services.TryAddSingleton(TimeProvider.System);

        services.AddHearthWebDefaults();
        services.AddHealthChecks().AddHomeLocation();
    }
}
