using Spotify.Records;
using SpotifyAPI.Web;
using ZiggyCreatures.Caching.Fusion;

namespace Spotify;

/// <summary>What's playing on the connected account. A null <see cref="Track"/> while connected means nothing is playing.</summary>
public sealed record NowPlayingResult(bool Connected, NowPlayingResponse? Track);

/// <summary>
/// Reads the connected account's current track, shared through a short cache so every screen polling
/// every few seconds costs one Spotify call between them. Only Spotify rejecting the saved login counts
/// as disconnected; rate limits and outages throw <see cref="APIException"/> for the caller to report
/// as unavailable, after fail-safe has served the last answer for a short while.
/// </summary>
public sealed class NowPlayingService(SpotifyClientService clients, IFusionCache cache)
{
    private const string CacheKey = "spotify:now-playing";

    // Short enough to follow a track change, long enough for several screens to share a call.
    // Fail-safe covers a brief blip; much longer and a stopped track would linger on screen.
    private static readonly FusionCacheEntryOptions CacheOptions = new()
    {
        Duration = TimeSpan.FromSeconds(4),
        IsFailSafeEnabled = true,
        FailSafeMaxDuration = TimeSpan.FromSeconds(30),
        FailSafeThrottleDuration = TimeSpan.FromSeconds(5),
    };

    public async Task<NowPlayingResult> GetAsync(CancellationToken ct = default)
    {
        var client = clients.TryGetClient();
        if (client is null) return new NowPlayingResult(false, null);

        return await cache.GetOrSetAsync<NowPlayingResult>(
            CacheKey,
            async (_, token) =>
            {
                try
                {
                    var playing = await client.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest(), token);
                    return new NowPlayingResult(true, ToResponse(playing));
                }
                catch (APIUnauthorizedException)
                {
                    // Revoked or unrefreshable login: an answer, not a failure for fail-safe to hide
                    return new NowPlayingResult(false, null);
                }
            },
            CacheOptions,
            ct);
    }

    private static NowPlayingResponse? ToResponse(CurrentlyPlaying? playing)
    {
        if (playing?.Item is not FullTrack track) return null;

        return new NowPlayingResponse(
            track.Name,
            string.Join(", ", track.Artists.Select(a => a.Name)),
            track.Album.Name,
            track.Album.Images.FirstOrDefault()?.Url,
            playing.ProgressMs ?? 0,
            track.DurationMs,
            playing.IsPlaying);
    }
}
