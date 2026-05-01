namespace Photos.Records;

public record PhotoResponse(
    string Id,
    string Url,
    string? Description,
    string PhotographerName,
    string UnsplashLink
);
