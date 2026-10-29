using Photos.Records;

namespace Photos;

public interface IPhotoSource
{
    string Key { get; }
    Task<PhotoResponse?> GetRandomAsync(PhotoSourceContext ctx);
}

/// <param name="Exclude">Photo ids to skip, most recently shown last (the frame's pass through Unsplash's photos).</param>
public record PhotoSourceContext(string Orientation, string? Query, IReadOnlyList<string>? Exclude = null);
