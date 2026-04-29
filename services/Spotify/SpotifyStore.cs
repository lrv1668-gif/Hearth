using System.Data.Common;
using Data.Abstractions;

namespace Spotify;

public sealed class SpotifyStore([FromKeyedServices("spotify")] IDatabase db)
{
    public void Migrate() => db.NonQuery("""
        CREATE TABLE IF NOT EXISTS spotify_tokens (
            id            INTEGER  PRIMARY KEY CHECK (id = 1),
            access_token  TEXT     NOT NULL,
            refresh_token TEXT     NOT NULL,
            expires_at    DATETIME NOT NULL
        )
        """);

    public SpotifyToken? Load() =>
        db.QueryOne(
            "SELECT access_token, refresh_token, expires_at FROM spotify_tokens WHERE id = 1",
            Map);

    public void Save(string accessToken, string refreshToken, DateTime expiresAt) =>
        db.NonQuery("""
            INSERT OR REPLACE INTO spotify_tokens (id, access_token, refresh_token, expires_at)
            VALUES (1, $access_token, $refresh_token, $expires_at)
            """, cmd =>
        {
            cmd.AddParam("$access_token", accessToken);
            cmd.AddParam("$refresh_token", refreshToken);
            cmd.AddParam("$expires_at", expiresAt.ToString("o"));
        });

    public void Clear() =>
        db.NonQuery("DELETE FROM spotify_tokens WHERE id = 1");

    private static SpotifyToken Map(DbDataReader r) =>
        new(r.GetString(0), r.GetString(1), r.GetDateTime(2));
}

public record SpotifyToken(string AccessToken, string RefreshToken, DateTime ExpiresAt);
