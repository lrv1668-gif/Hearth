using System.Data.Common;
using Data.Abstractions;

namespace Calendar;

public sealed class CalendarStore([FromKeyedServices("calendar")] IDatabase db)
{
    public void Migrate()
    {
        db.NonQuery("""
            CREATE TABLE IF NOT EXISTS calendar_tokens (
                provider      TEXT PRIMARY KEY,
                access_token  TEXT NOT NULL,
                refresh_token TEXT NOT NULL,
                expires_at    TEXT NOT NULL
            )
            """);

        db.NonQuery("""
            CREATE TABLE IF NOT EXISTS calendar_items_cache (
                provider    TEXT PRIMARY KEY,
                items_json  TEXT NOT NULL,
                cached_at   TEXT NOT NULL
            )
            """);
    }

    public bool HasToken(string provider) =>
        db.QueryOne(
            "SELECT 1 FROM calendar_tokens WHERE provider = $p",
            _ => true,
            c => c.AddParam("$p", provider)) is true;

    public CalendarToken? LoadToken(string provider) =>
        db.QueryOne(
            "SELECT access_token, refresh_token, expires_at FROM calendar_tokens WHERE provider = $p",
            MapToken,
            c => c.AddParam("$p", provider));

    public void SaveToken(string provider, string accessToken, string refreshToken, DateTimeOffset expiresAt) =>
        db.NonQuery("""
            INSERT OR REPLACE INTO calendar_tokens (provider, access_token, refresh_token, expires_at)
            VALUES ($provider, $access_token, $refresh_token, $expires_at)
            """, cmd =>
        {
            cmd.AddParam("$provider", provider);
            cmd.AddParam("$access_token", accessToken);
            cmd.AddParam("$refresh_token", refreshToken);
            cmd.AddParam("$expires_at", expiresAt.ToString("o"));
        });

    public (string Json, DateTimeOffset CachedAt)? LoadItemsCache(string provider) =>
        db.QueryOne<(string Json, DateTimeOffset CachedAt)?>(
            "SELECT items_json, cached_at FROM calendar_items_cache WHERE provider = $p",
            r => (r.Field<string>("items_json")!, DateTimeOffset.Parse(r.Field<string>("cached_at")!)),
            c => c.AddParam("$p", provider));

    public void SaveItemsCache(string provider, string json) =>
        db.NonQuery("""
            INSERT OR REPLACE INTO calendar_items_cache (provider, items_json, cached_at)
            VALUES ($provider, $json, $cached_at)
            """, cmd =>
        {
            cmd.AddParam("$provider", provider);
            cmd.AddParam("$json", json);
            cmd.AddParam("$cached_at", DateTimeOffset.UtcNow.ToString("o"));
        });

    public void InvalidateItemsCache(string provider) =>
        db.NonQuery(
            "DELETE FROM calendar_items_cache WHERE provider = $p",
            c => c.AddParam("$p", provider));

    // Two sequential deletes — IDatabase has no transaction API.
    // A crash between them leaves a stale cache row but no token row, which is
    // harmless: GetItemsAsync checks IsAuthenticated before reading the cache.
    public void Clear(string provider)
    {
        db.NonQuery(
            "DELETE FROM calendar_tokens WHERE provider = $p",
            c => c.AddParam("$p", provider));
        db.NonQuery(
            "DELETE FROM calendar_items_cache WHERE provider = $p",
            c => c.AddParam("$p", provider));
    }

    private static CalendarToken MapToken(DbDataReader r) =>
        new(
            r.Field<string>("access_token")!,
            r.Field<string>("refresh_token")!,
            DateTimeOffset.Parse(r.Field<string>("expires_at")!));
}

public record CalendarToken(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);
