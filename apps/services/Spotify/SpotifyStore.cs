using System.Data.Common;
using System.Security.Cryptography;
using Data.Abstractions;
using Microsoft.AspNetCore.DataProtection;

namespace Spotify;

public sealed class SpotifyStore([FromKeyedServices("spotify")] IDatabase db, IDataProtectionProvider dpProvider)
{
    private readonly IDataProtector _protector = dpProvider.CreateProtector("Spotify.Tokens.v1");

    public void Migrate()
    {
        db.NonQuery("""
            CREATE TABLE IF NOT EXISTS spotify_tokens (
                id            INTEGER  PRIMARY KEY CHECK (id = 1),
                access_token  TEXT     NOT NULL,
                refresh_token TEXT     NOT NULL,
                expires_at    DATETIME NOT NULL
            )
            """);

        // The scopes Spotify granted; NULL for logins saved before it was recorded
        try { db.NonQuery("ALTER TABLE spotify_tokens ADD COLUMN scope TEXT"); }
        catch (DbException ex) when (ex.Message.Contains("duplicate column name", StringComparison.OrdinalIgnoreCase))
        {
            // column already exists from a previous run
        }
    }

    public SpotifyToken? Load() =>
        db.QueryOne(
            "SELECT access_token, refresh_token, expires_at, scope FROM spotify_tokens WHERE id = 1",
            Map);

    public void Save(string accessToken, string refreshToken, DateTime expiresAt, string? scope) =>
        db.NonQuery("""
            INSERT OR REPLACE INTO spotify_tokens (id, access_token, refresh_token, expires_at, scope)
            VALUES (1, $access_token, $refresh_token, $expires_at, $scope)
            """, cmd =>
        {
            cmd.AddParam("$access_token", _protector.Protect(accessToken));
            cmd.AddParam("$refresh_token", _protector.Protect(refreshToken));
            cmd.AddParam("$expires_at", expiresAt.ToString("o"));
            cmd.AddParam("$scope", scope);
        });

    public void Clear() =>
        db.NonQuery("DELETE FROM spotify_tokens WHERE id = 1");

    // Rows written before token encryption was introduced won't decrypt — treat
    // them as absent so callers fall back to re-auth instead of a 500.
    private SpotifyToken? Map(DbDataReader r)
    {
        try
        {
            return new SpotifyToken(
                _protector.Unprotect(r.Field<string>("access_token")!),
                _protector.Unprotect(r.Field<string>("refresh_token")!),
                r.Field<DateTime>("expires_at"),
                r.Field<string>("scope"));
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}

public record SpotifyToken(string AccessToken, string RefreshToken, DateTime ExpiresAt, string? Scope)
{
    /// <summary>Whether this login was granted every scope Hearth now asks for.</summary>
    public bool HasRequiredScopes =>
        Scope?.Split(' ', StringSplitOptions.RemoveEmptyEntries) is { } granted
        && SpotifyScopes.Required.All(granted.Contains);
}
