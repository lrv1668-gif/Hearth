using System.Text.Json;

namespace Photos;

/// <summary>
/// Named sets of uploads ("Winter", "Family", …) for picking several photos at once.
/// Kept in groups.json beside captions.json; a group only lists uploads that still exist.
/// </summary>
public sealed class PhotoGroupStore
{
    public const int MaxNameLength = 40;
    public const int MaxGroups = 50;

    private readonly string _path;
    private readonly UploadStore _uploads;
    private readonly Lock _lock = new();
    private List<PhotoGroup> _groups;

    public PhotoGroupStore(IConfiguration config, UploadStore uploads)
    {
        _uploads = uploads;
        _path = Path.Combine(config["DATA_PATH"] ?? "/data", "groups.json");
        _groups = File.Exists(_path)
            ? JsonSerializer.Deserialize<List<PhotoGroup>>(File.ReadAllText(_path)) ?? []
            : [];
    }

    public IReadOnlyList<PhotoGroup> List()
    {
        var known = _uploads.List().Select(p => p.Id).ToHashSet();
        lock (_lock)
        {
            return _groups.Select(g => g with { PhotoIds = g.PhotoIds.Where(known.Contains).ToList() }).ToList();
        }
    }

    /// <summary>Null with an error when the name or count is out of bounds.</summary>
    public (PhotoGroup? Group, string? Error) Create(PhotoGroupInput input)
    {
        if (Validate(input) is { } error) return (null, error);
        lock (_lock)
        {
            if (_groups.Count >= MaxGroups) return (null, $"at most {MaxGroups} groups");
            var group = new PhotoGroup(Guid.NewGuid().ToString("N")[..12], input.Name.Trim(), KnownIds(input.PhotoIds));
            _groups.Add(group);
            Persist();
            return (group, null);
        }
    }

    /// <summary>Not found when there's no such group; an error when the input is out of bounds.</summary>
    public (bool Found, PhotoGroup? Group, string? Error) Update(string id, PhotoGroupInput input)
    {
        if (Validate(input) is { } error) return (true, null, error);
        lock (_lock)
        {
            var i = _groups.FindIndex(g => g.Id == id);
            if (i < 0) return (false, null, null);
            _groups[i] = _groups[i] with { Name = input.Name.Trim(), PhotoIds = KnownIds(input.PhotoIds) };
            Persist();
            return (true, _groups[i], null);
        }
    }

    public bool Delete(string id)
    {
        lock (_lock)
        {
            if (_groups.RemoveAll(g => g.Id == id) == 0) return false;
            Persist();
            return true;
        }
    }

    /// <summary>Drops a deleted upload from every group.</summary>
    public void RemovePhoto(string photoId)
    {
        lock (_lock)
        {
            if (!_groups.Any(g => g.PhotoIds.Contains(photoId))) return;
            _groups = _groups.Select(g => g with { PhotoIds = g.PhotoIds.Where(p => p != photoId).ToList() }).ToList();
            Persist();
        }
    }

    private static string? Validate(PhotoGroupInput input)
    {
        var name = input.Name?.Trim();
        if (string.IsNullOrEmpty(name)) return "name required";
        if (name.Length > MaxNameLength) return $"name must be at most {MaxNameLength} characters";
        return null;
    }

    private List<string> KnownIds(IReadOnlyList<string>? ids)
    {
        var known = _uploads.List().Select(p => p.Id).ToHashSet();
        return (ids ?? []).Where(known.Contains).Distinct().ToList();
    }

    private void Persist() => File.WriteAllText(_path, JsonSerializer.Serialize(_groups));
}

public record PhotoGroup(string Id, string Name, IReadOnlyList<string> PhotoIds);

public record PhotoGroupInput(string Name, IReadOnlyList<string>? PhotoIds);
