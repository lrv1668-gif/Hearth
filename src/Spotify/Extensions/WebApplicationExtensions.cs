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
                    "SPOTIFY_REDIRECT_URI", "SPOTIFY_CLIENT_ID", "SPOTIFY_CLIENT_SECRET") is { } configError)
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
                Scope = SpotifyScopes.Required,
                State = state,
            };

            return Results.Redirect(loginRequest.ToUri().ToString());
        });

        app.MapGet("/spotify/callback", async (
            string code,
            string state,
            SpotifyClientService clientService,
            LastPlayedService lastPlayed,
            IConfiguration config) =>
        {
            if (config.RequireOrFail(
                    app.Logger,
                    _ => Results.Json(new { error = "spotify not configured" }, statusCode: 503),
                    "SPOTIFY_REDIRECT_URI", "SPOTIFY_CLIENT_SECRET") is { } configError)
            {
                return configError;
            }

            if (!pendingStates.TryRemove(state, out var expiry) || expiry < DateTimeOffset.UtcNow)
                return Results.BadRequest("invalid or expired state");

            // RequireOrFail above guarantees this is present.
            await clientService.SaveTokensFromCode(code, config["SPOTIFY_REDIRECT_URI"]!);
            await lastPlayed.ForgetAsync();

            var frontendUrl = config["FRONTEND_URL"] ?? "/";
            return Results.Redirect(frontendUrl);
        });

        // 401 only when there's no usable login; a rate limit or outage is a 502, so the
        // dashboard keeps the track it has rather than showing Spotify as disconnected
        app.MapGet("/spotify/now-playing", async (NowPlayingService nowPlaying, CancellationToken ct) =>
        {
            try
            {
                var result = await nowPlaying.GetAsync(ct);
                if (!result.Connected) return Results.Unauthorized();
                return result.Track is null ? Results.NoContent() : Results.Ok(result.Track);
            }
            catch (Exception ex) when ((ex is APIException or HttpRequestException or TaskCanceledException)
                                       && !ct.IsCancellationRequested)
            {
                app.Logger.LogWarning(ex, "Spotify now-playing unavailable");
                return Results.Json(new { error = "spotify unavailable" }, statusCode: 502);
            }
        });

        // For the frame: 401 when not linked, 403 when the login predates the history scopes
        // (Settings asks to reconnect), 204 with no history yet, 502 when Spotify can't be reached
        app.MapGet("/spotify/recent", async (LastPlayedService lastPlayed, CancellationToken ct) =>
        {
            try
            {
                var result = await lastPlayed.GetAsync(ct);
                return result.State switch
                {
                    LastPlayedState.Disconnected   => Results.Unauthorized(),
                    LastPlayedState.NeedsReconnect => Results.Json(new { error = "reconnect spotify" }, statusCode: 403),
                    _ => result.Response is { Track: null, TopArtist: null } ? Results.NoContent() : Results.Ok(result.Response),
                };
            }
            catch (APIUnauthorizedException)
            {
                return Results.Unauthorized();
            }
            catch (APIException ex) when (LastPlayedService.IsMissingScope(ex))
            {
                return Results.Json(new { error = "reconnect spotify" }, statusCode: 403);
            }
            catch (Exception ex) when ((ex is APIException or HttpRequestException or TaskCanceledException)
                                       && !ct.IsCancellationRequested)
            {
                app.Logger.LogWarning(ex, "Spotify listening history unavailable");
                return Results.Json(new { error = "spotify unavailable" }, statusCode: 502);
            }
        });

        app.MapGet("/spotify/status", (SpotifyStore store) =>
        {
            var token = store.Load();
            return Results.Ok(new StatusResponse(token is not null, token is { HasRequiredScopes: false }));
        });

        app.MapDelete("/spotify/auth", async (SpotifyStore store, LastPlayedService lastPlayed) =>
        {
            store.Clear();
            await lastPlayed.ForgetAsync();
            return Results.NoContent();
        });
    }
}
