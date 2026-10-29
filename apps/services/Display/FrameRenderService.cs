using System.Text.Json;
using System.Text.Json.Serialization;
using Display.Records;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using ServiceDefaults;

namespace Display;

/// <summary>What GET /display/frame/status reports: the render side, plus the frame client's last poll.</summary>
public sealed record FrameStatus(
    string? Mode, DateTimeOffset? RenderedAt, DateTimeOffset? CheckedAt, DateTimeOffset? NextRenderAt,
    // "etag", not snake_case's "e_tag"
    [property: JsonPropertyName("etag")] string? ETag,
    string? LastError,
    FramePoll? LastPoll = null);

/// <summary>
/// Renders the panel's frame in the background, so GET /display/frame serves a cached
/// PNG with a stable ETag instead of a fresh capture per poll (a Spectra 6 refresh
/// flashes for ~30 s, so the frame should change only when its content does).
/// <para>
/// A render is due when nothing is cached yet, the resolved mode (or its pinned photo)
/// differs from the cached frame's, the mode's <c>Until</c> passes, or — since the last
/// render — <see cref="FlatInterval"/> has passed for the flat modes (byte-identical
/// output keeps the ETag, so the panel doesn't redraw) or the photo interval for Ambient
/// and Photo. <see cref="Invalidate"/> (Settings, mode and override changes) renders now;
/// changes made during a render are coalesced into one more. A failed render keeps the
/// last frame and retries after <see cref="RetryDelay"/> (<see cref="FirstRetryDelay"/>
/// while nothing is cached, e.g. the renderer is still starting), or as soon as the mode
/// moves on. Ambient and Photo capture the photo <see cref="FramePhotos"/> holds, so the
/// panel matches Settings' preview; only an interval render, <see cref="Refresh"/> or coming
/// back to the mode once the photo is a photo interval old moves it on to the next one. Inside Settings → Frame's quiet hours the interval renders wait for them to end
/// (<see cref="QuietHours"/>), and a rotating schedule rule holds its step until they end.
/// A failure reading the settings or schedule is logged and retried; it never stops the service.
/// </para>
/// </summary>
public sealed class FrameRenderService(
    FrameCache cache,
    FrameSettingsStore settings,
    HomeSettingsStore home,
    FramePhotos photos,
    ModeStore modes,
    IServiceProvider services,
    IConfiguration config,
    TimeProvider clock,
    ILogger<FrameRenderService> logger) : BackgroundService
{
    public static readonly TimeSpan FlatInterval = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan FirstRetryDelay = TimeSpan.FromSeconds(10);

    private readonly Lock _lock = new();
    private readonly SemaphoreSlim _wake = new(0);
    // Completed by the render that picks up the latest Invalidate (queued), and by the one running now
    private TaskCompletionSource? _queued;
    private TaskCompletionSource? _running;
    private RenderFailure? _failure;
    private string? _lastError;
    private DateTimeOffset? _nextRenderAt;
    private bool _nextPhoto;
    // The photo the cached frame shows, so a newer pick (a held upload deleted) re-renders
    private string? _capturedPhotoId;

    /// <summary>Asks for a render as soon as the current one (if any) finishes.</summary>
    public void Invalidate()
    {
        lock (_lock) _queued ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _wake.Release();
    }

    /// <summary>Renders now with the next photo — the frame's Button A.</summary>
    public void Refresh()
    {
        lock (_lock) _nextPhoto = true;
        Invalidate();
    }

    /// <summary>Re-renders when <paramref name="mode"/> is on the panel with another photo than <paramref name="photoId"/>.</summary>
    public void PhotoPicked(string mode, string photoId)
    {
        bool stale;
        // Also when the panel captured none (Photos was down) and one is available now
        lock (_lock) stale = _capturedPhotoId != photoId;
        if (stale && cache.Current?.Info.Mode == mode) Invalidate();
    }

    /// <summary>Completes once every change invalidated so far has been rendered (or failed to).</summary>
    public Task Pending
    {
        get
        {
            lock (_lock) return (_queued ?? _running)?.Task ?? Task.CompletedTask;
        }
    }

    public FrameStatus Status()
    {
        var info = cache.Current?.Info;
        lock (_lock)
            return new FrameStatus(info?.Mode, info?.RenderedAt, info?.CheckedAt, _nextRenderAt, info?.ETag, _lastError);
    }

    /// <summary>
    /// When the next render is due — <see cref="DateTimeOffset.MinValue"/> for now — given
    /// the cached frame, the mode that should be on the panel and the last failure. An
    /// interval render that would land in the quiet hours waits for them to end; a mode
    /// change doesn't (e.g. a Night rule that starts with them still draws).
    /// </summary>
    public static DateTimeOffset Due(
        RenderedFrame? last, ResolvedMode mode, FrameSettings? saved, RenderFailure? failure,
        DateTimeOffset now, TimeZoneInfo zone)
    {
        // Back off while the same frame keeps failing; a mode that has since moved on gets
        // one attempt now (its failure then backs off in turn, so this can't spin)
        if (failure is not null && failure.Mode == mode.Mode && failure.PhotoId == mode.PhotoId)
            return failure.At + (last is null ? FirstRetryDelay : RetryDelay);
        if (last is null || last.Mode != mode.Mode || last.PhotoId != mode.PhotoId) return DateTimeOffset.MinValue;
        var interval = mode.Mode is FrameModes.Ambient or FrameModes.Photo
            ? TimeSpan.FromMinutes(saved?.PhotoIntervalMinutes ?? FrameSettings.DefaultPhotoIntervalMinutes)
            : FlatInterval;
        var next = last.CheckedAt + interval;
        if (QuietHours.EndOf(saved, next > now ? next : now, zone) is { } quietEnd) next = quietEnd;
        return mode.Until is { } until && until < next ? until : next;
    }

    /// <summary>
    /// The /frame URL to capture: the resolved mode spelled out, so the dither always matches
    /// it, and the held photo (a pinned upload included), so the panel shows the one the preview does.
    /// </summary>
    public static string FramePath(ResolvedMode mode, FramePhoto? photo = null)
    {
        var query = new QueryBuilder { { "mode", mode.Mode } };
        if (photo is not null) query.Add("photo", JsonSerializer.Serialize(photo, HearthJson.SnakeCaseLower));
        return "/frame" + query.ToQueryString();
    }

    /// <summary>
    /// Whether a render moves the photo on: Button A, or the interval render of a frame
    /// already showing this mode — not a Settings change, a mode change or a retry, which
    /// keep the photo the preview shows.
    /// </summary>
    public static bool NextPhoto(RenderedFrame? last, ResolvedMode mode, bool invalidated, bool refresh, RenderFailure? failure) =>
        refresh || (!invalidated && failure is null && last is not null && last.Mode == mode.Mode && last.PhotoId == mode.PhotoId);

    /// <summary>
    /// Coming into Ambient or Photo from another mode (a schedule boundary, a rotating rule),
    /// a held photo at least one photo interval old moves on — the photo the preview just
    /// showed doesn't. Null (no limit) while the mode stays.
    /// </summary>
    public static TimeSpan? PhotoMaxAge(RenderedFrame? last, ResolvedMode mode, FrameSettings? saved) =>
        last is not null && last.Mode == mode.Mode
            ? null
            : TimeSpan.FromMinutes(saved?.PhotoIntervalMinutes ?? FrameSettings.DefaultPhotoIntervalMinutes);

    /// <summary>The mode's method, or none when the frame client dithers (Settings → Frame).</summary>
    public static string DitherFor(ResolvedMode mode, FrameSettings? saved) =>
        saved?.Dithering == "frame" ? "none" : FramePreview.DitherFor(mode.Mode);

    /// <summary>The last failed render: when, and which frame it was for.</summary>
    public sealed record RenderFailure(DateTimeOffset At, string Mode, string? PhotoId);

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        // Nothing cached yet: the first GET /display/frame waits for this render, not a 503
        if (cache.Current is null) Invalidate();
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        if (config.Missing("RENDERER_URL").Count > 0)
        {
            logger.LogError("RENDERER_URL must be set. Update the .env file to configure. GET /display/frame is unavailable.");
            ReleaseWaiters();
            return;
        }

        while (!stop.IsCancellationRequested)
        {
            try
            {
                var now = clock.GetUtcNow();
                var zone = HomeZones.Resolve(home.Get(), config);
                var saved = settings.Get();
                var mode = ModeResolver.Resolve(modes.GetSchedule(), modes.GetOverride(), now, zone, saved);
                bool queued;
                DateTimeOffset due;
                lock (_lock)
                {
                    queued = _queued is not null;
                    due = Due(cache.Current?.Info, mode, saved, _failure, now, zone);
                    _nextRenderAt = queued || due <= now ? now : due;
                }

                if (queued || due <= now) await RenderAsync(mode, stop);
                else await WaitAsync(due - now, stop);
            }
            catch (Exception ex) when (!stop.IsCancellationRequested)
            {
                // E.g. a database error reading the schedule: don't take the whole service down
                logger.LogError(ex, "Frame render loop failed; retrying");
                lock (_lock) _lastError = ex.Message;
                ReleaseWaiters();
                await WaitAsync(RetryDelay, stop);
            }
        }
    }

    /// <summary>Lets anyone waiting on <see cref="Pending"/> go — nothing will render for them now.</summary>
    private void ReleaseWaiters()
    {
        TaskCompletionSource? queued;
        lock (_lock)
        {
            queued = _queued;
            _queued = null;
        }
        queued?.TrySetResult();
    }

    private ResolvedMode Resolve(DateTimeOffset now) =>
        ModeResolver.Resolve(modes.GetSchedule(), modes.GetOverride(), now, HomeZones.Resolve(home.Get(), config), settings.Get());

    private async Task RenderAsync(ResolvedMode mode, CancellationToken stop)
    {
        TaskCompletionSource? waiting;
        bool nextPhoto;
        var last = cache.Current?.Info;
        lock (_lock)
        {
            waiting = _running = _queued;
            _queued = null;
            nextPhoto = NextPhoto(last, mode, waiting is not null, _nextPhoto, _failure);
            _nextPhoto = false;
        }

        try
        {
            // Resolve again: an invalidating change (e.g. Button B) may have just moved the mode
            if (waiting is not null) mode = Resolve(clock.GetUtcNow());
            var saved = settings.Get();
            var (width, height) = FramePreview.Canvas(QueryCollection.Empty, saved);
            var rotate = width < height ? saved?.PortraitRotation ?? FrameSettings.DefaultPortraitRotation : 0;
            var photo = mode.Mode is FrameModes.Ambient or FrameModes.Photo
                ? await photos.GetAsync(mode.Mode, saved, mode.PhotoId, nextPhoto, PhotoMaxAge(last, mode, saved), stop)
                : null;
            var png = await services.GetRequiredService<RendererClient>().RenderAsync(
                FramePath(mode, photo), width, height, DitherFor(mode, saved), kernel: null, look: "screen",
                home.Get()?.TimeZone, rotate, stop);
            var info = cache.Store(png, mode.Mode, mode.PhotoId, clock.GetUtcNow());
            logger.LogInformation("Rendered the {Mode} frame ({ETag})", mode.Mode, info.ETag);
            lock (_lock)
            {
                _failure = null;
                _lastError = null;
                _capturedPhotoId = photo?.Id;
            }
        }
        catch (Exception ex) when (!stop.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Frame render failed; keeping the last frame");
            lock (_lock)
            {
                _failure = new RenderFailure(clock.GetUtcNow(), mode.Mode, mode.PhotoId);
                _lastError = ex.Message;
            }
        }
        finally
        {
            lock (_lock) _running = null;
            waiting?.TrySetResult();
        }
    }

    /// <summary>Sleeps for <paramref name="delay"/> or until <see cref="Invalidate"/>, whichever is first.</summary>
    private async Task WaitAsync(TimeSpan delay, CancellationToken stop)
    {
        using var done = CancellationTokenSource.CreateLinkedTokenSource(stop);
        await Task.WhenAny(Task.Delay(delay, clock, done.Token), _wake.WaitAsync(done.Token));
        await done.CancelAsync();
    }

    public override void Dispose()
    {
        _wake.Dispose();
        base.Dispose();
    }
}
