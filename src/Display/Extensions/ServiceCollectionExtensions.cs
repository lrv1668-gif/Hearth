using Data.Extensions;
using ServiceDefaults;

namespace Display.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForDisplay(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSqliteDatabase("display", "display.db");
        serviceCollection.AddSingleton<FrameSettingsStore>();
        serviceCollection.AddHttpClient<RendererClient>((services, http) =>
        {
            // RENDERER_URL is checked per request (503 when unset), so don't fail here
            if (services.GetRequiredService<IConfiguration>()["RENDERER_URL"] is { Length: > 0 } url)
                http.BaseAddress = new Uri(url.EndsWith('/') ? url : url + "/");
            // The renderer waits up to 30 s for the frame to be ready, plus the capture
            http.Timeout = TimeSpan.FromSeconds(60);
        });

        serviceCollection.AddHearthWebDefaults();
    }
}
