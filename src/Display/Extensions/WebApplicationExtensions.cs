using System.Net;
using Display.Records;
using Microsoft.AspNetCore.Mvc;
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
        app.Services.GetRequiredService<ModeStore>().Migrate();
        app.AddDisplayEndpoints();
        app.AddModeEndpoints();
    }

    /// <summary>
    /// What the frame shows: a manual override (with an optional expiry) beats the
    /// time-of-day schedule, which beats its default mode. /frame without ?mode= asks
    /// GET /display/mode; the frame's Button B calls POST /display/mode/next.
    /// </summary>
    private static void AddModeEndpoints(this WebApplication app)
    {
        app.MapGet("/display/mode", (ModeStore store, HomeSettingsStore home, IConfiguration config, TimeProvider clock) =>
            Results.Ok(ModeResolver.Resolve(store.GetSchedule(), store.GetOverride(), clock.GetUtcNow(), HomeZone(home, config))));

        app.MapGet("/display/mode/schedule", (ModeStore store) => Results.Ok(store.GetSchedule()));

        app.MapPut("/display/mode/schedule", (ModeSchedule schedule, ModeStore store) =>
            schedule.Validate() is { } error ? Results.BadRequest(error) : Results.Ok(store.SaveSchedule(schedule)));

        app.MapPut("/display/mode/override", (ModeOverride manual, ModeStore store, TimeProvider clock) =>
        {
            if (manual.Validate() is { } error) return Results.BadRequest(error);
            if (manual.Until <= clock.GetUtcNow()) return Results.BadRequest("until must be in the future");
            return Results.Ok(store.SetOverride(manual));
        });

        // Back to the schedule. With photo_id, only when that photo is the one pinned —
        // the frontend calls this after deleting an upload, so a pin never outlives its photo.
        app.MapDelete("/display/mode/override", ([FromQuery(Name = "photo_id")] string? photoId, ModeStore store) =>
        {
            if (photoId is null || store.GetOverride()?.PhotoId == photoId) store.ClearOverride();
            return Results.NoContent();
        });

        // Step to the next mode, holding it until the schedule's next change.
        app.MapPost("/display/mode/next", (ModeStore store, HomeSettingsStore home, IConfiguration config, TimeProvider clock) =>
        {
            var now = clock.GetUtcNow();
            var zone = HomeZone(home, config);
            var schedule = store.GetSchedule();
            var current = ModeResolver.Resolve(schedule, store.GetOverride(), now, zone);
            store.SetOverride(new ModeOverride(FrameModes.Next(current.Mode), ModeResolver.NextBoundary(schedule, now, zone)));
            return Results.Ok(ModeResolver.Resolve(schedule, store.GetOverride(), now, zone));
        });
    }

    /// <summary>The household's zone from Settings → Home, else TZ, else the host's.</summary>
    private static TimeZoneInfo HomeZone(HomeSettingsStore home, IConfiguration config)
    {
        var id = home.Get()?.TimeZone ?? config["TZ"];
        return !string.IsNullOrWhiteSpace(id) && TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone)
            ? zone
            : TimeZoneInfo.Local;
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
