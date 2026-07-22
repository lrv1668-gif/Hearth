using System.Collections.Concurrent;
using ServiceDefaults;
using Spotify;
using Spotify.Records;
using SpotifyAPI.Web;

namespace Spotify.Extensions;

public static class WebApplicationExtensions
{
    public static void InitializeWebAppForSpotify(this WebApplication app)
    {
        app.Services.GetRequiredService<SpotifyStore>().Migrate();
        app.AddSpotifyEndpoints();
    }

    private static void AddSpotifyEndpoints(this WebApplication app)
    {
        var pendingStates = new ConcurrentDictionary<string, DateTimeOffset>();

        app.MapGet("/spotify/auth", (IConfiguration config) =>
        {
            if (config.RequireOrFail(
                    app.Logger,
                    _ => Results.Json(new { error = "spotify not configured" }, statusCode: 503),
                    "SPOTIFY_REDIRECT_URI", "SPOTIFY_CLIENT_ID") is { } configError)
            {
                return configError;
            }

            var state = Guid.NewGuid().ToString("N");
            pendingStates[state] = DateTimeOffset.UtcNow.AddMinutes(10);

            // RequireOrFail above guarantees these are present.
            var loginRequest = new LoginRequest(
                new Uri(config["SPOTIFY_REDIRECT_URI"]!),
                config["SPOTIFY_CLIENT_ID"]!,
                LoginRequest.ResponseType.Code)
            {
                Scope = [Scopes.UserReadCurrentlyPlaying, Scopes.UserReadPlaybackState],
                State = state,
            };

            return Results.Redirect(loginRequest.ToUri().ToString());
        });

        app.MapGet("/spotify/callback", async (
            string code,
            string state,
            SpotifyClientService clientService,
            IConfiguration config) =>
        {
            if (config.RequireOrFail(
                    app.Logger,
                    _ => Results.Json(new { error = "spotify not configured" }, statusCode: 503),
                    "SPOTIFY_REDIRECT_URI") is { } configError)
            {
                return configError;
            }

            if (!pendingStates.TryRemove(state, out var expiry) || expiry < DateTimeOffset.UtcNow)
                return Results.BadRequest("invalid or expired state");

            // RequireOrFail above guarantees this is present.
            await clientService.SaveTokensFromCode(code, config["SPOTIFY_REDIRECT_URI"]!);

            var frontendUrl = config["FRONTEND_URL"] ?? "/";
            return Results.Redirect(frontendUrl);
        });

        app.MapGet("/spotify/now-playing", async (SpotifyClientService clientService) =>
        {
            var client = clientService.TryGetClient();
            if (client is null) return Results.Unauthorized();

            CurrentlyPlaying? playing;
            try
            {
                playing = await client.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest());
            }
            catch (APIException)
            {
                return Results.Unauthorized();
            }

            if (playing?.Item is not FullTrack track) return Results.NoContent();

            var artist   = string.Join(", ", track.Artists.Select(a => a.Name));
            var albumArt = track.Album.Images.FirstOrDefault()?.Url;

            return Results.Ok(new NowPlayingResponse(
                track.Name,
                artist,
                track.Album.Name,
                albumArt,
                playing.ProgressMs ?? 0,
                track.DurationMs,
                playing.IsPlaying));
        });

        app.MapGet("/spotify/status", (SpotifyClientService clientService) =>
            Results.Ok(new StatusResponse(clientService.TryGetClient() is not null)));

        app.MapDelete("/spotify/auth", (SpotifyStore store) =>
        {
            store.Clear();
            return Results.NoContent();
        });
    }
}
