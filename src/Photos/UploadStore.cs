using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using SkiaSharp;

namespace Photos;

public sealed class UploadStore
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    private const int FullMaxDim  = 2560;
    private const int ThumbMaxDim = 400;
    private const int WebpQuality = 85;

    private readonly string _uploadsDir;
    private readonly string _captionsPath;
    private readonly ConcurrentDictionary<string, UploadedPhoto> _index = new();
    private readonly ConcurrentDictionary<string, string> _captions = new();
    private readonly Lock _captionsFileLock = new();

    public UploadStore(IConfiguration config)
    {
        var dataPath = config["DATA_PATH"] ?? "/data";
        _uploadsDir = Path.Combine(dataPath, "uploads");
        Directory.CreateDirectory(_uploadsDir);

        _captionsPath = Path.Combine(dataPath, "captions.json");
        if (File.Exists(_captionsPath))
        {
            var stored = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(_captionsPath));
            foreach (var (id, caption) in stored ?? []) _captions[id] = caption;
        }

        foreach (var file in Directory.EnumerateFiles(_uploadsDir))
        {
            var filename = Path.GetFileName(file);
            if (filename.EndsWith("_thumb.webp", StringComparison.OrdinalIgnoreCase)) continue;

            var id = Path.GetFileNameWithoutExtension(filename);
            _index[id] = MakeRecord(id);
        }
    }

    public bool IsAllowedExtension(string ext) => AllowedExtensions.Contains(ext);

    public async Task<UploadedPhoto?> SaveAsync(IFormFile file)
    {
        byte[] bytes;
        await using (var stream = file.OpenReadStream())
        {
            bytes = new byte[file.Length];
            await stream.ReadExactlyAsync(bytes);
        }

        var id = Convert.ToHexString(SHA256.HashData(bytes))[..32].ToLowerInvariant();
        var record = MakeRecord(id);
        if (!_index.TryAdd(id, record)) return null; // duplicate — atomic gate

        using var bitmap = SKBitmap.Decode(bytes);
        if (bitmap is null)
        {
            _index.TryRemove(id, out _);
            throw new InvalidDataException("File could not be decoded as an image.");
        }

        try
        {
            await SaveResizedAsync(bitmap, id, FullMaxDim,  $"{id}.webp");
            await SaveResizedAsync(bitmap, id, ThumbMaxDim, $"{id}_thumb.webp");
            return record;
        }
        catch
        {
            _index.TryRemove(id, out _);
            TryDelete(Path.Combine(_uploadsDir, $"{id}.webp"));
            TryDelete(Path.Combine(_uploadsDir, $"{id}_thumb.webp"));
            throw;
        }
    }

    public IReadOnlyCollection<UploadedPhoto> List() => _index.Values.ToList();

    public bool TryGetRandom(out UploadedPhoto? photo)
    {
        var all = _index.Values.ToList();
        if (all.Count == 0) { photo = null; return false; }
        photo = all[Random.Shared.Next(all.Count)];
        return true;
    }

    public bool Delete(string id)
    {
        if (!_index.TryRemove(id, out _)) return false;
        TryDelete(Path.Combine(_uploadsDir, $"{id}.webp"));
        TryDelete(Path.Combine(_uploadsDir, $"{id}_thumb.webp"));
        if (_captions.TryRemove(id, out _)) PersistCaptions();
        return true;
    }

    public bool SetCaption(string id, string? caption)
    {
        if (!_index.ContainsKey(id)) return false;

        caption = string.IsNullOrWhiteSpace(caption) ? null : caption.Trim();
        if (caption is null) _captions.TryRemove(id, out _);
        else _captions[id] = caption;

        _index[id] = MakeRecord(id);
        PersistCaptions();
        return true;
    }

    private void PersistCaptions()
    {
        lock (_captionsFileLock)
        {
            File.WriteAllText(_captionsPath, JsonSerializer.Serialize(_captions.ToDictionary()));
        }
    }

    public string GetFilePath(string filename) => Path.Combine(_uploadsDir, filename);

    private async Task SaveResizedAsync(SKBitmap src, string id, int maxDim, string filename)
    {
        var (w, h) = Scale(src.Width, src.Height, maxDim);
        using var resized = src.Resize(new SKImageInfo(w, h), SKSamplingOptions.Default);
        using var image   = SKImage.FromBitmap(resized);
        using var data    = image.Encode(SKEncodedImageFormat.Webp, WebpQuality);
        await File.WriteAllBytesAsync(Path.Combine(_uploadsDir, filename), data.ToArray());
    }

    private static (int w, int h) Scale(int origW, int origH, int maxDim)
    {
        if (origW <= maxDim && origH <= maxDim) return (origW, origH);
        var scale = Math.Min((float)maxDim / origW, (float)maxDim / origH);
        return ((int)(origW * scale), (int)(origH * scale));
    }

    private static string UrlFor(string filename) => $"/photos/files/{filename}";

    private UploadedPhoto MakeRecord(string id) =>
        new(id, UrlFor($"{id}.webp"), UrlFor($"{id}_thumb.webp"), _captions.GetValueOrDefault(id));

    private static void TryDelete(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }
}

public record UploadedPhoto(string Id, string Url, string ThumbUrl, string? Caption);

public record BatchFileResult(string FileName, string Status, string? Error, UploadedPhoto? Photo);
