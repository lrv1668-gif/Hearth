namespace Display.Records;

/// <summary>
/// A photo the frame shows — the Photos service's GET /photos/random shape, and the
/// frontend's Photo (what /frame's ?photo= param takes).
/// </summary>
public sealed record FramePhoto(
    string  Id,
    string  Url,
    string? ThumbUrl,
    string? Description,
    string? PhotographerName,
    string? UnsplashLink,
    string  Source
);

/// <summary>One of the Photos service's uploads (GET /photos/uploads); its caption is the credit line.</summary>
public sealed record UploadedPhoto(string Id, string Url, string? ThumbUrl, string? Caption)
{
    public FramePhoto ToPhoto() => new(Id, Url, ThumbUrl, Caption, null, null, "local");
}
