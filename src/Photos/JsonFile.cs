using System.Text.Json;

namespace Photos;

/// <summary>
/// The small JSON files beside the uploads (captions.json, groups.json). Writes go through a
/// temp file and a rename, so a power cut mid-write leaves the old file rather than a torn one;
/// a file that still can't be read is set aside (<c>.bad</c>) instead of stopping the service.
/// </summary>
public static class JsonFile
{
    public static T? Read<T>(string path, ILogger logger) where T : class
    {
        if (!File.Exists(path)) return null;
        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path));
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // Kept for recovery by hand; the next write starts a fresh file
            logger.LogError(ex, "Couldn't read {Path} — starting empty and keeping it as {Path}.bad", path, path);
            try { File.Move(path, path + ".bad", overwrite: true); }
            catch (IOException) { }
            return null;
        }
    }

    public static void Write<T>(string path, T value)
    {
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(value));
        File.Move(tmp, path, overwrite: true);
    }
}
