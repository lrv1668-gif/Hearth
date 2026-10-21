using Display.Records;

namespace Display.Extensions;

public static class WebApplicationExtensions
{
    public static void InitializeWebAppForDisplay(this WebApplication app)
    {
        app.Services.GetRequiredService<FrameSettingsStore>().Migrate();
        app.AddDisplayEndpoints();
    }

    private static void AddDisplayEndpoints(this WebApplication app)
    {
        // The frame's shared settings. 204 until something has been saved — clients
        // then use their built-in defaults.
        app.MapGet("/display/settings", (FrameSettingsStore store) =>
            store.Get() is { } settings ? Results.Ok(settings) : Results.NoContent());

        app.MapPut("/display/settings", (FrameSettings settings, FrameSettingsStore store) =>
            settings.Validate() is { } error ? Results.BadRequest(error) : Results.Ok(store.Save(settings)));
    }
}
