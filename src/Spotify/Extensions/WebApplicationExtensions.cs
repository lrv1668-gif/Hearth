using System.Collections.Concurrent;
using Spotify;
using Spotify.Records;
using SpotifyAPI.Web;

namespace Spotify.Extensions;

public static class WebApplicationExtensions
{
    public static void InitializeWebAppForSpotify(this WebApplication app)
    {
        app.Services.GetRequiredService<SpotifyStore>().Migrate();

        var health = GetHealth(app.Configuration);
        if (health.Configured)
            app.Logger.LogInformation("Spotify configured — SPOTIFY_CLIENT_ID, SPOTIFY_CLIENT_SECRET, SPOTIFY_REDIRECT_URI set");
        else
            app.Logger.LogWarning(
                "Spotify not configured — missing {Missing}; /spotify/auth will return 503 until set in src/Spotify/.env",
                string.Join(", ", health.Missing));

        app.AddSpotifyEndpoints();
    }

    private static HealthResponse GetHealth(IConfiguration config) =>
        Health.Evaluate(
            ("SPOTIFY_CLIENT_ID", config["SPOTIFY_CLIENT_ID"]),
            ("SPOTIFY_CLIENT_SECRET", config["SPOTIFY_CLIENT_SECRET"]),
            ("SPOTIFY_REDIRECT_URI", config["SPOTIFY_REDIRECT_URI"]));

    private static void AddSpotifyEndpoints(this WebApplication app)
    {
        var pendingStates = new ConcurrentDictionary<string, DateTimeOffset>();

        app.MapGet("/spotify/health", (IConfiguration config) => Results.Ok(GetHealth(config)));

        app.MapGet("/spotify/auth", (IConfiguration config) =>
        {
            if (!GetHealth(config).Configured)
            {
                app.Logger.LogError(
                    "SPOTIFY_CLIENT_ID, SPOTIFY_CLIENT_SECRET, and SPOTIFY_REDIRECT_URI must be set. Update the .env file to add your Spotify app credentials.");
                return Results.Json(new { error = "spotify not configured" }, statusCode: 503);
            }

            var state = Guid.NewGuid().ToString("N");
            pendingStates[state] = DateTimeOffset.UtcNow.AddMinutes(10);

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
            if (!pendingStates.TryRemove(state, out var expiry) || expiry < DateTimeOffset.UtcNow)
                return Results.BadRequest("invalid or expired state");

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
