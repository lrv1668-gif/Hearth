namespace Spotify.Records;

/// <summary>The frame's Last Played card: the latest track in the listening history and this month's top artist.</summary>
public record LastPlayedResponse(RecentTrack? Track, string? TopArtist);

public record RecentTrack(
    string Title,
    string Artist,
    string AlbumName,
    string? AlbumArtUrl,
    DateTime PlayedAt
);
