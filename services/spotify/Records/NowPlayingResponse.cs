namespace Spotify.Records;

public record NowPlayingResponse(
    string Title,
    string Artist,
    string AlbumName,
    string? AlbumArtUrl,
    int ProgressMs,
    int DurationMs,
    bool IsPlaying
);
