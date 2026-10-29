using Photos.Records;

namespace Photos;

public interface IPhotoSource
{
    string Key { get; }
    Task<PhotoResponse?> GetRandomAsync(PhotoSourceContext ctx);
}

public record PhotoSourceContext(string Orientation, string? Query);
