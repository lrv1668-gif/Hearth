using Data.Extensions;
using ServiceDefaults;

namespace Display.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForDisplay(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSqliteDatabase("display", "display.db");
        serviceCollection.AddSingleton<FrameSettingsStore>();
        serviceCollection.AddSingleton<HomeSettingsStore>();
        serviceCollection.AddHearthCache();
        serviceCollection.AddHttpClient<GeocodingClient>();
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
