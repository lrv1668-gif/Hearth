using SpotifyAPI.Web;

namespace Spotify;

public static class SpotifyScopes
{
    /// <summary>
    /// What the login asks for: the live track for the dashboard, and listening history and
    /// top artists for the frame's Last Played card. A login saved with fewer needs reconnecting.
    /// </summary>
    public static readonly string[] Required =
    [
        Scopes.UserReadCurrentlyPlaying,
        Scopes.UserReadPlaybackState,
        Scopes.UserReadRecentlyPlayed,
        Scopes.UserTopRead,
    ];
}
