using System.Net;
using Spotify.Records;
using SpotifyAPI.Web;
using ZiggyCreatures.Caching.Fusion;

namespace Spotify;

public enum LastPlayedState { Disconnected, NeedsReconnect, Ok }

public sealed record LastPlayedResult(LastPlayedState State, LastPlayedResponse? Response = null);

/// <summary>
/// The connected account's listening history, for the e-paper frame. Cached for an hour: Display
/// re-renders every 15 minutes and every changed pixel redraws the whole panel, so the card
/// following each track through a listening session would flash the frame four times an hour.
/// The top artist moves over weeks, so it's cached longer, and a failed lookup only drops that
/// line. A rejected login throws <see cref="APIUnauthorizedException"/>, a missing scope an
/// <see cref="APIException"/> with 403, and outages an <see cref="APIException"/>, once fail-safe
/// has nothing left to serve.
/// </summary>
public sealed class LastPlayedService(
    SpotifyStore store, SpotifyClientService clients, IFusionCache cache, ILogger<LastPlayedService> logger)
{
    private const string RecentKey    = "spotify:recently-played";
    private const string TopArtistKey = "spotify:top-artist";

    private static readonly FusionCacheEntryOptions RecentOptions = new()
    {
        Duration = TimeSpan.FromHours(1),
        IsFailSafeEnabled = true,
        FailSafeMaxDuration = TimeSpan.FromDays(1),
        FailSafeThrottleDuration = TimeSpan.FromMinutes(5),
    };

    private static readonly FusionCacheEntryOptions TopArtistOptions = new()
    {
        Duration = TimeSpan.FromHours(12),
        IsFailSafeEnabled = true,
        FailSafeMaxDuration = TimeSpan.FromDays(7),
        FailSafeThrottleDuration = TimeSpan.FromMinutes(30),
    };

    // Album art at the frame's size: Spotify lists 640, 300 and 64px versions
    private const int ArtMinWidth = 300;

    public async Task<LastPlayedResult> GetAsync(CancellationToken ct = default)
    {
        var token = store.Load();
        if (token is null) return new LastPlayedResult(LastPlayedState.Disconnected);
        if (!token.HasRequiredScopes) return new LastPlayedResult(LastPlayedState.NeedsReconnect);

        var client = clients.TryGetClient();
        if (client is null) return new LastPlayedResult(LastPlayedState.Disconnected);

        var recent = cache.GetOrSetAsync<RecentTrack?>(
            RecentKey,
            async (_, t) =>
            {
                var history = await client.Player.GetRecentlyPlayed(new PlayerRecentlyPlayedRequest { Limit = 1 }, t);
                return history.Items?.FirstOrDefault() is { } item ? ToTrack(item) : null;
            },
            RecentOptions,
            ct).AsTask();
        var topArtist = TopArtistAsync(client, ct);

        return new LastPlayedResult(LastPlayedState.Ok, new LastPlayedResponse(await recent, await topArtist));
    }

    /// <summary>Drops the cached answers, so a new or removed login shows on the next render.</summary>
    public async Task ForgetAsync()
    {
        await cache.RemoveAsync(RecentKey);
        await cache.RemoveAsync(TopArtistKey);
    }

    private async Task<string?> TopArtistAsync(SpotifyClient client, CancellationToken ct)
    {
        try
        {
            return await cache.GetOrSetAsync<string?>(
                TopArtistKey,
                async (_, t) =>
                {
                    var top = await client.Personalization.GetTopArtists(
                        new PersonalizationTopRequest
                        {
                            Limit = 1,
                            TimeRangeParam = PersonalizationTopRequest.TimeRange.ShortTerm,
                        },
                        t);
                    return top.Items?.FirstOrDefault()?.Name;
                },
                TopArtistOptions,
                ct);
        }
        catch (Exception ex) when ((ex is APIException or HttpRequestException or TaskCanceledException)
                                   && !ct.IsCancellationRequested)
        {
            // Only a line under the card: show the track without it
            logger.LogWarning(ex, "Spotify top artist unavailable");
            return null;
        }
    }

    private static RecentTrack? ToTrack(PlayHistoryItem item)
    {
        if (item.Track is not { } track) return null;

        var images = track.Album.Images ?? [];
        var art = images.Where(i => i.Width >= ArtMinWidth).MinBy(i => i.Width) ?? images.FirstOrDefault();

        return new RecentTrack(
            track.Name,
            string.Join(", ", track.Artists.Select(a => a.Name)),
            track.Album.Name,
            art?.Url,
            DateTime.SpecifyKind(item.PlayedAt.ToUniversalTime(), DateTimeKind.Utc));
    }

    /// <summary>Spotify refused the call for a scope the login wasn't granted.</summary>
    public static bool IsMissingScope(APIException ex) => ex.Response?.StatusCode == HttpStatusCode.Forbidden;
}
