using System.Data.Common;
using Data.Abstractions;

namespace Photos;

public sealed class PhotoStore([FromKeyedServices("photos")] IDatabase db)
{
    public void Migrate() => db.NonQuery("""
        CREATE TABLE IF NOT EXISTS photos_cache (
            id          INTEGER PRIMARY KEY CHECK (id = 1),
            photos_json TEXT    NOT NULL,
            query       TEXT    NOT NULL,
            orientation TEXT    NOT NULL,
            fetched_at  TEXT    NOT NULL
        )
        """);

    public PhotoCache? Load() =>
        db.QueryOne(
            "SELECT photos_json, query, orientation, fetched_at FROM photos_cache WHERE id = 1",
            Map);

    public void Save(string photosJson, string query, string orientation) =>
        db.NonQuery("""
            INSERT OR REPLACE INTO photos_cache (id, photos_json, query, orientation, fetched_at)
            VALUES (1, $photos, $query, $orientation, $fetched_at)
            """, cmd =>
        {
            cmd.AddParam("$photos", photosJson);
            cmd.AddParam("$query", query);
            cmd.AddParam("$orientation", orientation);
            cmd.AddParam("$fetched_at", DateTime.UtcNow.ToString("o"));
        });

    public static bool IsStale(PhotoCache cache) =>
        !DateTime.TryParse(cache.FetchedAt, out var fetched) ||
        (DateTime.UtcNow - fetched).TotalHours > 24;

    public static bool IsQueryMatch(PhotoCache cache, string query) =>
        string.Equals(cache.Query, query, StringComparison.OrdinalIgnoreCase);

    public static bool IsOrientationMatch(PhotoCache cache, string orientation) =>
        string.Equals(cache.Orientation, orientation, StringComparison.OrdinalIgnoreCase);

    private static PhotoCache Map(DbDataReader r) =>
        new(r.Field<string>("photos_json")!, r.Field<string>("query")!, r.Field<string>("orientation")!, r.Field<string>("fetched_at")!);
}

public record PhotoCache(string PhotosJson, string Query, string Orientation, string FetchedAt);
