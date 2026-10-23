using Data.Extensions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceDefaults;

namespace Display.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForDisplay(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSqliteDatabase("display", "display.db");
        serviceCollection.AddSingleton<FrameSettingsStore>();
        serviceCollection.AddSingleton<HomeSettingsStore>();
        serviceCollection.AddSingleton<ModeStore>();
        // The panel's frame: rendered in the background, cached beside display.db
        serviceCollection.AddSingleton(services => new FrameCache(
            FrameCache.DirectoryFor(Environment.GetEnvironmentVariable("DB_PATH")),
            services.GetRequiredService<ILogger<FrameCache>>()));
        serviceCollection.AddSingleton<FrameRenderService>();
        serviceCollection.AddHostedService(services => services.GetRequiredService<FrameRenderService>());
        serviceCollection.TryAddSingleton(TimeProvider.System);
        serviceCollection.AddHearthCache();
        serviceCollection.AddHttpClient<GeocodingClient>(http => http.Timeout = TimeSpan.FromSeconds(10));
        serviceCollection.AddHttpClient<RendererClient>((services, http) =>
        {
            // RENDERER_URL is checked per request (503 when unset), so don't fail here
            if (services.GetRequiredService<IConfiguration>()["RENDERER_URL"] is { Length: > 0 } url)
                http.BaseAddress = new Uri(url.EndsWith('/') ? url : url + "/");
            // A capture has a 40 s deadline plus dithering, and one more may be ahead in the queue
            http.Timeout = TimeSpan.FromSeconds(110);
        });

        serviceCollection.AddHearthWebDefaults();
    }
}
