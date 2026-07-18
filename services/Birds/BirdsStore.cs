using System.Data.Common;
using System.Globalization;
using Data.Abstractions;

namespace Birds;

public sealed class BirdsStore([FromKeyedServices("birds")] IDatabase db)
{
    public void Migrate() => db.NonQuery("""
        CREATE TABLE IF NOT EXISTS birds_cache (
            id             INTEGER PRIMARY KEY CHECK (id = 1),
            sightings_json TEXT    NOT NULL,
            fetched_at     TEXT    NOT NULL
        )
        """);

    public BirdsCache? Load() =>
        db.QueryOne(
            "SELECT sightings_json, fetched_at FROM birds_cache WHERE id = 1",
            Map);

    public void Save(string sightingsJson) =>
        db.NonQuery("""
            INSERT OR REPLACE INTO birds_cache (id, sightings_json, fetched_at)
            VALUES (1, $sightings, $fetched_at)
            """, cmd =>
        {
            cmd.AddParam("$sightings", sightingsJson);
            cmd.AddParam("$fetched_at", DateTime.UtcNow.ToString("o"));
        });

    public static bool IsStale(BirdsCache cache)
    {
        // Parse with RoundtripKind so the stored UTC ("...Z") timestamp keeps Kind=Utc.
        // Default TryParse converts it to local time, which skews the comparison against
        // DateTime.UtcNow by the machine's UTC offset (cache reads as perpetually stale).
        if (!DateTime.TryParse(cache.FetchedAt, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var fetched)) return true;
        return (DateTime.UtcNow - fetched).TotalMinutes > 60;
    }

    private static BirdsCache Map(DbDataReader r) =>
        new(r.Field<string>("sightings_json")!, r.Field<string>("fetched_at")!);
}

public record BirdsCache(string SightingsJson, string FetchedAt);
