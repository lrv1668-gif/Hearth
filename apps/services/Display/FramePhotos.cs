using System.Globalization;
using System.Text.Json;
using Display.Records;
using ServiceDefaults;

namespace Display;

/// <summary>
/// The photo the frame shows in Ambient and in Photo mode, and in Daily's Photo widget — one
/// held for each, so the panel, Settings' preview and /frame in a browser all show the same
/// picture instead of each making its own random pick.
/// <para>
/// A held photo stays until its choice changes (Ambient: the source, categories and
/// orientation; Photo: the pinned upload or album), it's no longer uploaded, or a
/// <c>next</c> read asks for another — the panel's interval render and Button A, so the
/// preview follows the panel — or a <c>maxAge</c> read finds it older than that (the panel
/// coming back to Ambient after a schedule's Daily hours). Look changes (theme, font, card position) keep it. Photo
/// mode with nothing picked, or none still uploaded, shows Ambient's photo. Daily's photo is
/// drawn from Ambient's choice too, but held for the household's day (<see cref="GetDailyAsync"/>):
/// Daily re-renders every 15 minutes, so only a new day or Button A moves it on. A new pick is
/// dealt like a shuffled deck — one not yet shown this pass, until every photo has been — for
/// Photo mode's album, Ambient's uploads and Unsplash's photos alike (Daily shares Ambient's decks). Kept, with each deck's
/// pass, in <c>photos.json</c> beside display.db, so a restart doesn't put a new photo on the panel.
/// </para>
/// </summary>
public sealed class FramePhotos(PhotosClient photos, string directory, TimeProvider clock, ILogger<FramePhotos> logger)
{
    private static readonly JsonSerializerOptions Json = HearthJson.SnakeCaseLower;

    /// <summary>A held photo, the choice it was picked for and when.</summary>
    public sealed record Held(string Key, FramePhoto Photo, DateTimeOffset PickedAt);

    private sealed record Saved(Held? Ambient, Held? Photo, Dictionary<string, List<string>>? Shown, Held? Daily = null);

    /// <summary>The Unsplash ids remembered at most — its pool changes daily, so a pass may never finish.</summary>
    public const int MaxShownUnsplash = 60;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _loaded;
    private Held? _ambient;
    private Held? _photo;
    private Held? _daily;
    // Each deck's ids shown this pass, in order: "photo" (the album), "local" and "unsplash" (Ambient)
    private Dictionary<string, List<string>> _shown = [];

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

    /// <summary>
    /// The photo Daily's Photo widget shows on <paramref name="today"/> (the household's date): drawn
    /// from Ambient's choice, held for the day — a new day, a new choice or <paramref name="next"/>
    /// (Button A) picks another. Null when there's none, PHOTOS_URL is unset or the Photos service
    /// can't be reached with nothing held.
    /// </summary>
    public async Task<FramePhoto?> GetDailyAsync(
        FrameSettings? saved, DateOnly today, bool next = false, CancellationToken ct = default)
    {
        if (!photos.Configured) return null;
        await _gate.WaitAsync(ct);
        try
        {
            if (!_loaded) Load();
            var key = AmbientKey(saved) + "|" + today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var source = saved?.PhotoSource ?? FrameSettings.DefaultPhotoSource;
            IReadOnlyList<UploadedPhoto>? listed = null;
            async Task<IReadOnlyList<UploadedPhoto>> Uploads() => listed ??= await photos.UploadsAsync(ct);
            if (_daily is { } held && held.Key == key && !next &&
                (held.Photo.Source != "local" || (await Uploads()).Any(u => u.Id == held.Photo.Id)))
                return held.Photo;

            var uploads = source is "local" or "both" ? await Uploads() : [];
            // Yesterday's photo beats none when there's nothing new to pick
            if (await PickAsync(source, uploads, saved, _daily?.Photo.Id, ct) is not { } picked) return _daily?.Photo;
            _daily = new Held(key, picked, clock.GetUtcNow());
            Save();
            return picked;
        }
        catch (Exception ex) when ((ex is HttpRequestException or JsonException or TaskCanceledException) && !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Couldn't pick the frame's daily photo — keeping the last one");
            return _daily?.Photo;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>The household's date at <paramref name="now"/> — the day Daily's photo is held for.</summary>
    public static DateOnly Today(TimeZoneInfo zone, DateTimeOffset now) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, zone).DateTime);

    /// <summary>The ambient choice a held photo is picked for.</summary>
    public static string AmbientKey(FrameSettings? saved) => string.Join('|',
        saved?.PhotoSource ?? FrameSettings.DefaultPhotoSource,
        string.Join(',', saved?.PhotoCategories ?? []),
        saved?.Orientation ?? "landscape");

    /// <summary>The Unsplash query for the chosen categories — "nature" when none are chosen (photos.ts photoQuery).</summary>
    public static string Query(IReadOnlyList<string>? categories) =>
        categories is { Count: > 0 } ? string.Join(',', categories) : "nature";

    /// <summary>The photos Photos holds per Unsplash search (PhotoFetcher's count) — "both" weighs uploads against it.</summary>
    public const int UnsplashPool = 20;

    /// <summary>
    /// Which source to draw from: "both" picks uploads in proportion to how many there are
    /// against <see cref="UnsplashPool"/>, so every photo comes round about as often (photos.ts resolveSource).
    /// </summary>
    public static string ResolveSource(string source, int uploads, double random) =>
        source != "both" ? source : random < (double)uploads / (uploads + UnsplashPool) ? "local" : "unsplash";

    /// <summary>
    /// The next of <paramref name="from"/> in a shuffled pass through them all: one at random not in
    /// <paramref name="shown"/> (the pass so far, which it's added to). Once all have been shown a new
    /// pass starts, other than <paramref name="currentId"/> when there's a choice so it doesn't repeat.
    /// </summary>
    public static UploadedPhoto Deal(IReadOnlyList<UploadedPhoto> from, List<string> shown, string? currentId, Random random)
    {
        // A deck saved before passes were kept still counts the photo on show
        if (shown.Count == 0 && currentId is not null) shown.Add(currentId);
        shown.RemoveAll(id => from.All(u => u.Id != id)); // deleted, or no longer in the album
        var unseen = from.Where(u => !shown.Contains(u.Id)).ToList();
        if (unseen.Count == 0)
        {
            shown.Clear();
            unseen = from.Count > 1 ? from.Where(u => u.Id != currentId).ToList() : [.. from];
        }
        var picked = unseen[random.Next(unseen.Count)];
        shown.Add(picked.Id);
        return picked;
    }

    private List<string> Shown(string deck) => _shown.TryGetValue(deck, out var ids) ? ids : _shown[deck] = [];

    private async Task<FramePhoto?> PhotoModeAsync(
        FrameSettings? saved, string? pinnedId, Func<Held, bool> movesOn, DateTimeOffset now, CancellationToken ct)
    {
        IReadOnlyList<string> ids = pinnedId is not null ? [pinnedId] : saved?.PhotoIds ?? [];
        if (ids.Count == 0) return null;
        var chosen = (await photos.UploadsAsync(ct)).Where(u => ids.Contains(u.Id)).ToList();
        if (chosen.Count == 0) return null; // all deleted: Ambient's photo

        var key = string.Join(',', ids);
        if (_photo is { } held && held.Key == key && !movesOn(held) && chosen.Any(u => u.Id == held.Photo.Id)) return held.Photo;
        // A pin is the one photo, so it leaves the album's pass where it was
        var picked = pinnedId is not null ? chosen[0] : Deal(chosen, Shown("photo"), _photo?.Photo.Id, Random.Shared);
        _photo = new Held(key, picked.ToPhoto(), now);
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
        var picked = await PickAsync(source, uploads, saved, _ambient?.Photo.Id, ct);
        if (picked is null) return _ambient?.Key == key ? _ambient.Photo : null;
        _ambient = new Held(key, picked, now);
        Save();
        return picked;
    }

    /// <summary>A new pick from Ambient's choice — an upload or a photo from <paramref name="source"/> — other than <paramref name="currentId"/>.</summary>
    private async Task<FramePhoto?> PickAsync(
        string source, IReadOnlyList<UploadedPhoto> uploads, FrameSettings? saved, string? currentId, CancellationToken ct)
    {
        var from = ResolveSource(source, uploads.Count, Random.Shared.NextDouble());
        return from == "local"
            ? uploads.Count > 0 ? Deal(uploads, Shown("local"), currentId, Random.Shared).ToPhoto() : null
            : await UnsplashAsync(from, saved, ct);
    }

    /// <summary>
    /// A photo from <paramref name="source"/> not yet shown this pass — Photos skips the ones it's
    /// sent while others are left; getting one back that was shown means they all were, so a new pass starts.
    /// </summary>
    private async Task<FramePhoto?> UnsplashAsync(string source, FrameSettings? saved, CancellationToken ct)
    {
        var shown = Shown(source);
        var picked = await photos.RandomAsync(
            source, Query(saved?.PhotoCategories), saved?.Orientation ?? "landscape", shown, ct);
        if (picked is null) return null;
        if (shown.Contains(picked.Id)) shown.Clear();
        shown.Add(picked.Id);
        if (shown.Count > MaxShownUnsplash) shown.RemoveRange(0, shown.Count - MaxShownUnsplash);
        return picked;
    }

    private void Load()
    {
        _loaded = true;
        try
        {
            if (!File.Exists(FilePath)) return;
            var saved = JsonSerializer.Deserialize<Saved>(File.ReadAllText(FilePath), Json);
            (_ambient, _photo, _daily, _shown) = (saved?.Ambient, saved?.Photo, saved?.Daily, saved?.Shown ?? []);
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
            File.WriteAllBytes(tmp, JsonSerializer.SerializeToUtf8Bytes(new Saved(_ambient, _photo, _shown, _daily), Json));
            File.Move(tmp, FilePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Still held in memory; only a restart would lose it
            logger.LogWarning(ex, "Couldn't save the held photos to {Directory}", directory);
        }
    }
}
