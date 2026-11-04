using System.Text.Json;
using Data.Abstractions;
using Display.Records;

namespace Display;

/// <summary>
/// Persists the single shared <see cref="HouseholdSettings"/> record (one row, id = 1) —
/// the RSS feeds and transit stops the dashboard and the frame both read. Each list is a
/// JSON column, saved whole.
/// </summary>
public sealed class HouseholdSettingsStore([FromKeyedServices("display")] IDatabase db)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void Migrate() =>
        db.NonQuery("""
            CREATE TABLE IF NOT EXISTS household_settings (
                id          INTEGER  PRIMARY KEY CHECK (id = 1),
                rss_feeds   TEXT     NOT NULL DEFAULT '[]',
                train_stops TEXT     NOT NULL DEFAULT '[]',
                updated_at  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            )
            """);

    /// <summary>The saved settings, or null if none have been saved yet.</summary>
    public HouseholdSettings? Get() =>
        db.QueryOne("SELECT rss_feeds, train_stops FROM household_settings WHERE id = 1",
            r => new HouseholdSettings(
                JsonSerializer.Deserialize<RssFeedSetting[]>(r.GetString(0), Json) ?? [],
                JsonSerializer.Deserialize<TrainStopSetting[]>(r.GetString(1), Json) ?? []));

    public HouseholdSettings Save(HouseholdSettings settings)
    {
        db.NonQuery("""
            INSERT INTO household_settings (id, rss_feeds, train_stops, updated_at)
            VALUES (1, $rss_feeds, $train_stops, CURRENT_TIMESTAMP)
            ON CONFLICT (id) DO UPDATE SET
                rss_feeds = excluded.rss_feeds, train_stops = excluded.train_stops, updated_at = excluded.updated_at
            """, cmd =>
        {
            cmd.AddParam("$rss_feeds", JsonSerializer.Serialize(
                settings.RssFeeds.Select(f => f with { Title = f.Title.Trim(), Url = f.Url.Trim() }), Json));
            cmd.AddParam("$train_stops", JsonSerializer.Serialize(
                // A null filter shows every line; an empty one (Settings' "Deselect all") shows none.
                settings.TrainStops.Select(s => s with { Label = s.Label.Trim(), StopKey = s.StopKey.Trim() }), Json));
        });
        return Get()!;
    }
}
