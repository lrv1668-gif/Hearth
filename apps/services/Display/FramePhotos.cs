using System.Text.Json;
using Display.Records;
using ServiceDefaults;

namespace Display;

/// <summary>
/// The photo the frame shows in Ambient and in Photo mode — one held for each, so the
/// panel, Settings' preview and /frame in a browser all show the same picture instead of
/// each making its own random pick.
/// <para>
/// A held photo stays until its choice changes (Ambient: the source, categories and
/// orientation; Photo: the pinned upload or album), it's no longer uploaded, or a
/// <c>next</c> read asks for another — the panel's interval render and Button A, so the
/// preview follows the panel — or a <c>maxAge</c> read finds it older than that (the panel
/// coming back to Ambient after a schedule's Daily hours). Look changes (theme, font, card position) keep it. Photo
/// mode with nothing picked, or none still uploaded, shows Ambient's photo. Kept in
/// <c>photos.json</c> beside display.db, so a restart doesn't put a new photo on the panel.
/// </para>
/// </summary>
public sealed class FramePhotos(PhotosClient photos, string directory, TimeProvider clock, ILogger<FramePhotos> logger)
{
    private static readonly JsonSerializerOptions Json = HearthJson.SnakeCaseLower;

    /// <summary>A held photo, the choice it was picked for and when.</summary>
    public sealed record Held(string Key, FramePhoto Photo, DateTimeOffset PickedAt);

    private sealed record Saved(Held? Ambient, Held? Photo);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _loaded;
    private Held? _ambient;
    private Held? _photo;

    private string FilePath => Path.Combine(directory, "photos.json");

    /// <summary>
    /// The photo <paramref name="mode"/> (ambient or photo) shows: the held one, or a new
    /// pick when <paramref name="next"/> or it no longer fits the choice. Null when there's
    /// none, PHOTOS_URL is unset or the Photos service can't be reached with nothing held.
    /// </summary>
    /// <param name="pinnedId">Photo mode's upload pinned with "Show on frame", which beats the saved album.</param>
    /// <param name="maxAge">Pick again if the held photo is at least this old.</param>
    public async Task<FramePhoto?> GetAsync(
        string mode, FrameSettings? saved, string? pinnedId = null, bool next = false, TimeSpan? maxAge = null,
        CancellationToken ct = default)
    {
        if (!photos.Configured) return null;
        await _gate.WaitAsync(ct);
        try
        {
            if (!_loaded) Load();
            var now = clock.GetUtcNow();
            bool MovesOn(Held held) => next || (maxAge is { } age && now - held.PickedAt >= age);
            if (mode == FrameModes.Photo && await PhotoModeAsync(saved, pinnedId, MovesOn, now, ct) is { } picked) return picked;
            return await AmbientAsync(saved, MovesOn, now, ct);
        }
        catch (Exception ex) when ((ex is HttpRequestException or JsonException or TaskCanceledException) && !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Couldn't pick the frame's {Mode} photo — keeping the last one", mode);
            return mode == FrameModes.Photo ? _photo?.Photo ?? _ambient?.Photo : _ambient?.Photo;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>The ambient choice a held photo is picked for.</summary>
    public static string AmbientKey(FrameSettings? saved) => string.Join('|',
        saved?.PhotoSource ?? FrameSettings.DefaultPhotoSource,
        string.Join(',', saved?.PhotoCategories ?? []),
        saved?.Orientation ?? "landscape");

    /// <summary>The Unsplash query for the chosen categories — "nature" when none are chosen (photos.ts photoQuery).</summary>
    public static string Query(IReadOnlyList<string>? categories) =>
        categories is { Count: > 0 } ? string.Join(',', categories) : "nature";

    /// <summary>Which source to draw from: "both" picks uploads half the time, when there are any (photos.ts resolveSource).</summary>
    public static string ResolveSource(string source, bool hasUploads, double random) =>
        source != "both" ? source : hasUploads && random < 0.5 ? "local" : "unsplash";

    /// <summary>One of <paramref name="from"/> at random, other than <paramref name="currentId"/> when there's a choice.</summary>
    public static UploadedPhoto Pick(IReadOnlyList<UploadedPhoto> from, string? currentId, Random random)
    {
        var others = from.Count > 1 ? from.Where(u => u.Id != currentId).ToList() : from;
        return others[random.Next(others.Count)];
    }

    private async Task<FramePhoto?> PhotoModeAsync(
        FrameSettings? saved, string? pinnedId, Func<Held, bool> movesOn, DateTimeOffset now, CancellationToken ct)
    {
        IReadOnlyList<string> ids = pinnedId is not null ? [pinnedId] : saved?.PhotoIds ?? [];
        if (ids.Count == 0) return null;
        var chosen = (await photos.UploadsAsync(ct)).Where(u => ids.Contains(u.Id)).ToList();
        if (chosen.Count == 0) return null; // all deleted: Ambient's photo

        var key = string.Join(',', ids);
        if (_photo is { } held && held.Key == key && !movesOn(held) && chosen.Any(u => u.Id == held.Photo.Id)) return held.Photo;
        _photo = new Held(key, Pick(chosen, _photo?.Photo.Id, Random.Shared).ToPhoto(), now);
        Save();
        return _photo.Photo;
    }

    private async Task<FramePhoto?> AmbientAsync(
        FrameSettings? saved, Func<Held, bool> movesOn, DateTimeOffset now, CancellationToken ct)
    {
        var key = AmbientKey(saved);
        var source = saved?.PhotoSource ?? FrameSettings.DefaultPhotoSource;
        // Listed only when needed: to check a held upload still exists, or to pick from them
        IReadOnlyList<UploadedPhoto>? listed = null;
        async Task<IReadOnlyList<UploadedPhoto>> Uploads() => listed ??= await photos.UploadsAsync(ct);
        if (_ambient is { } held && held.Key == key && !movesOn(held) &&
            (held.Photo.Source != "local" || (await Uploads()).Any(u => u.Id == held.Photo.Id)))
            return held.Photo;

        var uploads = source is "local" or "both" ? await Uploads() : [];
        var from = ResolveSource(source, uploads.Count > 0, Random.Shared.NextDouble());
        var picked = from == "local"
            ? uploads.Count > 0 ? Pick(uploads, _ambient?.Photo.Id, Random.Shared).ToPhoto() : null
            : await photos.RandomAsync(from, Query(saved?.PhotoCategories), saved?.Orientation ?? "landscape", ct);
        if (picked is null) return _ambient?.Key == key ? _ambient.Photo : null;
        _ambient = new Held(key, picked, now);
        Save();
        return picked;
    }

    private void Load()
    {
        _loaded = true;
        try
        {
            if (!File.Exists(FilePath)) return;
            var saved = JsonSerializer.Deserialize<Saved>(File.ReadAllText(FilePath), Json);
            (_ambient, _photo) = (saved?.Ambient, saved?.Photo);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Ignoring the unreadable held photos in {Directory}", directory);
        }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(directory);
            var tmp = FilePath + ".tmp";
            File.WriteAllBytes(tmp, JsonSerializer.SerializeToUtf8Bytes(new Saved(_ambient, _photo), Json));
            File.Move(tmp, FilePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Still held in memory; only a restart would lose it
            logger.LogWarning(ex, "Couldn't save the held photos to {Directory}", directory);
        }
    }
}
