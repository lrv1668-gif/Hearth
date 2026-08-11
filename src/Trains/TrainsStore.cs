using System.Data.Common;
using System.Globalization;
using Data.Abstractions;

namespace Trains;

public sealed class TrainsStore([FromKeyedServices("trains")] IDatabase db)
{
    public void Migrate() => db.NonQuery("""
        CREATE TABLE IF NOT EXISTS trains_cache (
            stop_key        TEXT PRIMARY KEY,
            departures_json TEXT NOT NULL,
            fetched_at      TEXT NOT NULL
        )
        """);

    public bool IsStale(string stopKey)
    {
        var fetchedAt = db.QueryOne(
            "SELECT fetched_at FROM trains_cache WHERE stop_key = $stop_key",
            r => r.Field<string>("fetched_at"),
            cmd => cmd.AddParam("$stop_key", stopKey));

        // Parse with RoundtripKind so the stored UTC ("...Z") timestamp keeps Kind=Utc.
        // Default TryParse converts it to local time, which skews the comparison against
        // DateTime.UtcNow by the machine's UTC offset (cache reads as perpetually stale).
        if (fetchedAt is null || !DateTime.TryParse(fetchedAt, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var fetched)) return true;
        return (DateTime.UtcNow - fetched).TotalSeconds > 45;
    }

    public string? Load(string stopKey) =>
        db.QueryOne(
            "SELECT departures_json FROM trains_cache WHERE stop_key = $stop_key",
            r => r.Field<string>("departures_json"),
            cmd => cmd.AddParam("$stop_key", stopKey));

    public void Save(string stopKey, string departuresJson) =>
        db.NonQuery("""
            INSERT OR REPLACE INTO trains_cache (stop_key, departures_json, fetched_at)
            VALUES ($stop_key, $departures, $fetched_at)
            """, cmd =>
        {
            cmd.AddParam("$stop_key", stopKey);
            cmd.AddParam("$departures", departuresJson);
            cmd.AddParam("$fetched_at", DateTime.UtcNow.ToString("o"));
        });
}
