using System.Security.Cryptography;
using System.Text.Json;
using ServiceDefaults;

namespace Display;

/// <summary>
/// What the last panel render produced. <paramref name="ETag"/> is a hash of the PNG, so
/// it only changes when the pixels do; <paramref name="RenderedAt"/> is when they last
/// changed and <paramref name="CheckedAt"/> when a render last ran.
/// </summary>
public sealed record RenderedFrame(
    string ETag, string Mode, string? PhotoId, DateTimeOffset RenderedAt, DateTimeOffset CheckedAt);

/// <summary>
/// The frame GET /display/frame serves, kept in memory and in <c>frame.png</c> +
/// <c>frame.json</c> beside display.db. A restart (e.g. a host update) then serves the
/// same picture and ETag and keeps the render schedule, rather than flashing a new
/// photo onto the panel.
/// </summary>
public sealed class FrameCache(string directory, ILogger<FrameCache> logger)
{
    private static readonly JsonSerializerOptions Json = HearthJson.SnakeCaseLower;

    private readonly Lock _lock = new();
    private bool _loaded;
    private (RenderedFrame Info, byte[] Png)? _frame;
    private TaskCompletionSource _changed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private string PngPath => Path.Combine(directory, "frame.png");
    private string InfoPath => Path.Combine(directory, "frame.json");

    /// <summary>The directory beside the SQLite file at <paramref name="dbPath"/> (DB_PATH).</summary>
    public static string DirectoryFor(string? dbPath) =>
        Path.GetDirectoryName(Path.GetFullPath(string.IsNullOrWhiteSpace(dbPath) ? "display.db" : dbPath))!;

    /// <summary>The last rendered frame, or null before the first render.</summary>
    public (RenderedFrame Info, byte[] Png)? Current
    {
        get
        {
            lock (_lock)
            {
                if (!_loaded) Load();
                return _frame;
            }
        }
    }

    /// <summary>Completes the next time the pixels (and so the ETag) change — what a long poll waits on.</summary>
    public Task Changed
    {
        get
        {
            lock (_lock) return _changed.Task;
        }
    }

    /// <summary>Keeps a new render. Pixels identical to the cached ones keep their ETag and RenderedAt.</summary>
    public RenderedFrame Store(byte[] png, string mode, string? photoId, DateTimeOffset at)
    {
        var etag = ETagFor(png);
        lock (_lock)
        {
            if (!_loaded) Load();
            var unchanged = _frame is { } current && current.Info.ETag == etag;
            var info = unchanged
                ? _frame!.Value.Info with { Mode = mode, PhotoId = photoId, CheckedAt = at }
                : new RenderedFrame(etag, mode, photoId, at, at);
            _frame = (info, unchanged ? _frame!.Value.Png : png);
            Save(info, unchanged ? null : png);
            if (!unchanged)
            {
                // Continuations run asynchronously, so waking the long polls here can't re-enter the lock
                _changed.TrySetResult();
                _changed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }
            return info;
        }
    }

    private static string ETagFor(byte[] png) => $"\"{Convert.ToHexStringLower(SHA256.HashData(png))[..16]}\"";

    private void Load()
    {
        _loaded = true;
        try
        {
            if (!File.Exists(InfoPath) || !File.Exists(PngPath)) return;
            var info = JsonSerializer.Deserialize<RenderedFrame>(File.ReadAllText(InfoPath), Json);
            var png = File.ReadAllBytes(PngPath);
            // The two files are written one after the other; a crash between them would pair
            // new pixels with the old ETag, and a client holding it would never see them
            if (info is not null && info.ETag == ETagFor(png)) _frame = (info, png);
            else logger.LogWarning("Ignoring the cached frame in {Directory}: it doesn't match its ETag", directory);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Ignoring the unreadable cached frame in {Directory}", directory);
        }
    }

    private void Save(RenderedFrame info, byte[]? png)
    {
        try
        {
            Directory.CreateDirectory(directory);
            if (png is not null) WriteAtomically(PngPath, png);
            WriteAtomically(InfoPath, JsonSerializer.SerializeToUtf8Bytes(info, Json));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Still served from memory; only a restart would lose it
            logger.LogWarning(ex, "Couldn't save the frame to {Directory}", directory);
        }
    }

    private static void WriteAtomically(string path, byte[] bytes)
    {
        var tmp = path + ".tmp";
        File.WriteAllBytes(tmp, bytes);
        File.Move(tmp, path, overwrite: true);
    }
}
