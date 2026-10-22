using System.Net;
using Display.Records;
using ServiceDefaults;
using ZiggyCreatures.Caching.Fusion;

namespace Display.Extensions;

public static class WebApplicationExtensions
{
    // Place names don't move; a long cache spares the geocoder repeat searches
    private static readonly FusionCacheEntryOptions PlacesCacheOptions = new()
    {
        Duration = TimeSpan.FromDays(7),
        IsFailSafeEnabled = true,
        FailSafeMaxDuration = TimeSpan.FromDays(30),
    };

    public static void InitializeWebAppForDisplay(this WebApplication app)
    {
        app.Services.GetRequiredService<FrameSettingsStore>().Migrate();
        app.Services.GetRequiredService<HomeSettingsStore>().Migrate();
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

        // The household's location and time zone (Settings → Home), shared with Weather,
        // Birds and Almanac. 204 until saved — they then use their env vars.
        app.MapGet("/display/home", (HomeSettingsStore store) =>
            store.Get() is { } home ? Results.Ok(home) : Results.NoContent());

        app.MapPut("/display/home", (HomeSettings home, HomeSettingsStore store) =>
            home.Validate() is { } error ? Results.BadRequest(error) : Results.Ok(store.Save(home)));

        // City search for picking the home location without typing coordinates.
        app.MapGet("/display/places", async (
            string? q, GeocodingClient geocoder, IFusionCache cache, CancellationToken ct) =>
        {
            var query = q?.Trim() ?? "";
            if (query.Length is < 2 or > 100) return Results.BadRequest("q must be 2–100 characters");
            try
            {
                var places = await cache.GetOrSetAsync<List<Place>>(
                    $"places:{query.ToLowerInvariant()}",
                    (_, token) => geocoder.SearchAsync(query, token), PlacesCacheOptions, ct);
                return Results.Ok(places);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                app.Logger.LogError(ex, "Place search failed");
                return Results.Json(new { error = "place search failed" }, statusCode: 502);
            }
        });

        // The frame dithered to the panel's six inks — the same pixels the panel gets.
        // Takes /frame's params, plus look=screen|paper and an optional error-diffusion kernel.
        app.MapGet("/display/preview", async (
            HttpRequest request, RendererClient renderer, FrameSettingsStore store, HomeSettingsStore home,
            IConfiguration config, CancellationToken ct) =>
        {
            if (config.RequireOrFail(
                    app.Logger,
                    _ => Results.Json(new { error = "renderer not configured" }, statusCode: 503),
                    "RENDERER_URL") is { } configError)
            {
                return configError;
            }

            var look = request.Query["look"].ToString() is "paper" ? "paper" : "screen";
            var (width, height) = FramePreview.Canvas(request.Query, store.Get());
            try
            {
                var png = await renderer.RenderAsync(
                    FramePreview.FramePath(request.Query), width, height,
                    FramePreview.DitherFor(request.Query["mode"]), request.Query["kernel"].FirstOrDefault(), look,
                    // The household's zone, so the frame's date and clock aren't drawn in UTC
                    home.Get()?.TimeZone, ct);
                return Results.File(png, "image/png");
            }
            catch (HttpRequestException ex)
            {
                app.Logger.LogWarning(ex, "Frame render failed");
                // Pass on a bad request (e.g. an unknown kernel), "busy" and "never got ready";
                // anything else is a bad gateway
                return Results.StatusCode(ex.StatusCode is HttpStatusCode.BadRequest
                    or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout
                    ? (int)ex.StatusCode
                    : 502);
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                app.Logger.LogWarning("Frame render timed out");
                return Results.StatusCode(504);
            }
        });
    }
}
