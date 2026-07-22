namespace Photos.Records;

public record PhotoResponse(
    string  Id,
    string  Url,
    string? ThumbUrl,
    string? Description,
    string? PhotographerName,
    string? UnsplashLink,
    string  Source
);
