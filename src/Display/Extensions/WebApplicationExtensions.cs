using System.Net;
using Display.Records;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
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

    // Long enough for a render queued behind another (see RendererClient's timeout), short
    // enough for inkypull's 120 s request timeout
    private static readonly TimeSpan WaitForRender = TimeSpan.FromSeconds(100);

    public static void InitializeWebAppForDisplay(this WebApplication app)
    {
        app.Services.GetRequiredService<FrameSettingsStore>().Migrate();
        app.Services.GetRequiredService<HomeSettingsStore>().Migrate();
        app.Services.GetRequiredService<ModeStore>().Migrate();
        app.AddDisplayEndpoints();
        app.AddModeEndpoints();
        app.AddFrameEndpoints();
        app.MapHearthHealth("display");
    }

    /// <summary>
    /// The panel's frame, rendered on a schedule by <see cref="FrameRenderService"/> — what
    /// a frame client (inkypull) polls. A change that invalidates it (Button B, a Settings
    /// edit) is waited for, so the poll right after a button press gets the new frame.
    /// </summary>
    private static void AddFrameEndpoints(this WebApplication app)
    {
        app.MapGet("/display/frame", async (
            FrameCache cache, FrameRenderService renderer, FramePolls polls, IConfiguration config,
            TimeProvider clock, HttpRequest request, HttpResponse response, CancellationToken ct) =>
        {
            if (config.RequireOrFail(
                    app.Logger,
                    _ => Results.Json(new { error = "renderer not configured" }, statusCode: 503),
                    "RENDERER_URL") is { } configError)
            {
                return configError;
            }

            try
            {
                await renderer.Pending.WaitAsync(WaitForRender, clock, ct);
            }
            catch (TimeoutException)
            {
                // Serve what's cached; the next poll picks the change up
            }

            var frame = cache.Current;
            // Whatever the answer, the frame checked in; a 304 leaves it holding the same ETag
            polls.Record(new FramePoll(clock.GetUtcNow(), ClientAddress(request),
                request.Headers.UserAgent.ToString() is { Length: > 0 } agent ? agent : null, frame?.Info.ETag));
            if (frame is not { } current)
            {
                response.Headers.RetryAfter = "60";
                return Results.Json(new { error = "no frame rendered yet" }, statusCode: 503);
            }
            response.Headers.CacheControl = "no-cache";
            // With an ETag, a matching If-None-Match gets a 304
            return Results.File(current.Png, "image/png",
                lastModified: current.Info.RenderedAt, entityTag: EntityTagHeaderValue.Parse(current.Info.ETag));
        });

        app.MapGet("/display/frame/status", (FrameRenderService renderer, FramePolls polls) =>
            Results.Ok(renderer.Status() with { LastPoll = polls.Last }));

        // Render now — e.g. a long press on the frame's Button A
        app.MapPost("/display/frame/refresh", (FrameRenderService renderer) =>
        {
            renderer.Invalidate();
            return Results.Accepted();
        });
    }

    /// <summary>
    /// Who asked: Caddy's X-Forwarded-For (its first hop) when proxied, else the connection's
    /// address. Only shown in Settings, never trusted for anything.
    /// </summary>
    private static string? ClientAddress(HttpRequest request) =>
        request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim() is { Length: > 0 } forwarded
            ? forwarded
            : request.HttpContext.Connection.RemoteIpAddress?.ToString();

    /// <summary>
    /// What the frame shows: a manual override (with an optional expiry) beats the
    /// time-of-day schedule, which beats its default mode. /frame without ?mode= asks
    /// GET /display/mode; the frame's Button B calls POST /display/mode/next.
    /// </summary>
    private static void AddModeEndpoints(this WebApplication app)
    {
        app.MapGet("/display/mode", (
            ModeStore store, FrameSettingsStore settings, HomeSettingsStore home, IConfiguration config, TimeProvider clock) =>
            Results.Ok(ModeResolver.Resolve(
                store.GetSchedule(), store.GetOverride(), clock.GetUtcNow(), HomeZones.Resolve(home.Get(), config), settings.Get())));

        app.MapGet("/display/mode/schedule", (ModeStore store) => Results.Ok(store.GetSchedule()));

        app.MapPut("/display/mode/schedule", (ModeSchedule schedule, ModeStore store, FrameRenderService frame) =>
        {
            if (schedule.Validate() is { } error) return Results.BadRequest(error);
            var saved = store.SaveSchedule(schedule);
            frame.Invalidate();
            return Results.Ok(saved);
        });

        app.MapPut("/display/mode/override", (ModeOverride manual, ModeStore store, TimeProvider clock, FrameRenderService frame) =>
        {
            if (manual.Validate() is { } error) return Results.BadRequest(error);
            if (manual.Until <= clock.GetUtcNow()) return Results.BadRequest("until must be in the future");
            var saved = store.SetOverride(manual);
            frame.Invalidate();
            return Results.Ok(saved);
        });

        // Back to the schedule. With photo_id, only when that photo is the one pinned —
        // the frontend calls this after deleting an upload, so a pin never outlives its photo.
        app.MapDelete("/display/mode/override", (
            [FromQuery(Name = "photo_id")] string? photoId, ModeStore store, FrameRenderService frame) =>
        {
            if (photoId is null || store.GetOverride()?.PhotoId == photoId)
            {
                store.ClearOverride();
                frame.Invalidate();
            }
            return Results.NoContent();
        });

        // Step to the next mode, holding it until the schedule's next change — a rule's
        // window ending or a rotating rule's next step, so the rotation picks up again.
        app.MapPost("/display/mode/next", (
            ModeStore store, FrameSettingsStore settings, HomeSettingsStore home, IConfiguration config,
            TimeProvider clock, FrameRenderService frame) =>
        {
            var now = clock.GetUtcNow();
            var zone = HomeZones.Resolve(home.Get(), config);
            var schedule = store.GetSchedule();
            var quiet = settings.Get();
            var current = ModeResolver.Resolve(schedule, store.GetOverride(), now, zone, quiet);
            var scheduled = ModeResolver.Resolve(schedule, null, now, zone, quiet);
            store.SetOverride(new ModeOverride(FrameModes.Next(current.Mode), scheduled.Until));
            frame.Invalidate();
            return Results.Ok(ModeResolver.Resolve(schedule, store.GetOverride(), now, zone, quiet));
        });
    }

    private static void AddDisplayEndpoints(this WebApplication app)
    {
        // The frame's shared settings. 204 until something has been saved — clients
        // then use their built-in defaults.
        app.MapGet("/display/settings", (FrameSettingsStore store) =>
            store.Get() is { } settings ? Results.Ok(settings) : Results.NoContent());

        app.MapPut("/display/settings", (FrameSettings settings, FrameSettingsStore store, FrameRenderService frame) =>
        {
            if (settings.Validate() is { } error) return Results.BadRequest(error);
            var saved = store.Save(settings);
            frame.Invalidate();
            return Results.Ok(saved);
        });

        // The household's location and time zone (Settings → Home), shared with Weather,
        // Birds and Almanac. 204 until saved — they then use their env vars.
        app.MapGet("/display/home", (HomeSettingsStore store) =>
            store.Get() is { } home ? Results.Ok(home) : Results.NoContent());

        app.MapPut("/display/home", (HomeSettings home, HomeSettingsStore store, FrameRenderService frame) =>
        {
            if (home.Validate() is { } error) return Results.BadRequest(error);
            var saved = store.Save(home);
            frame.Invalidate();
            return Results.Ok(saved);
        });

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
        // Takes /frame's params, plus look=screen|paper, an optional error-diffusion kernel and
        // dither= to override the mode's method (none: the raw screenshot).
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
                    FramePreview.DitherFor(request.Query), request.Query["kernel"].FirstOrDefault(), look,
                    // The household's zone, so the frame's date and clock aren't drawn in UTC
                    home.Get()?.TimeZone, ct: ct);
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
