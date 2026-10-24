namespace Spotify.Records;

/// <summary><see cref="NeedsReconnect"/>: linked, but before Hearth asked for the scopes Last Played needs.</summary>
public record StatusResponse(bool Authenticated, bool NeedsReconnect);
