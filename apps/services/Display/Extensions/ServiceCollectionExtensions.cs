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
        // The photo Ambient and Photo show, held beside the cached frame
        serviceCollection.AddSingleton(services => new FramePhotos(
            services.GetRequiredService<PhotosClient>(),
            FrameCache.DirectoryFor(Environment.GetEnvironmentVariable("DB_PATH")),
            services.GetRequiredService<TimeProvider>(),
            services.GetRequiredService<ILogger<FramePhotos>>()));
        serviceCollection.AddSingleton<FrameRenderService>();
        serviceCollection.AddSingleton<FramePolls>();
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

        serviceCollection.AddHttpClient<PhotosClient>((services, http) =>
        {
            // Checked per request (503 when unset) and by the health check, like RENDERER_URL
            if (services.GetRequiredService<IConfiguration>()["PHOTOS_URL"] is { Length: > 0 } url)
                http.BaseAddress = new Uri(url.EndsWith('/') ? url : url + "/");
            http.Timeout = TimeSpan.FromSeconds(10);
        });

        serviceCollection.AddHearthWebDefaults();
        serviceCollection.AddHealthChecks().AddDatabase("display").AddRequiredConfig("RENDERER_URL", "PHOTOS_URL").AddCheck<FrameHealthCheck>("frame");
    }
}
