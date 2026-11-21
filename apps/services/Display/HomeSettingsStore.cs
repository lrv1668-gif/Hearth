using System.Data.Common;
using Data.Abstractions;
using ServiceDefaults;

namespace Display;

/// <summary>
/// Persists the single shared <see cref="HomeSettings"/> record (one row, id = 1) — the
/// household's location, time zone and weather units, read by Weather, Birds, Almanac and the renderer.
/// Kept apart from frame_settings: the frame's settings are saved whole on every change.
/// </summary>
public sealed class HomeSettingsStore([FromKeyedServices("display")] IDatabase db)
{
    public void Migrate()
    {
        db.NonQuery("""
            CREATE TABLE IF NOT EXISTS home_settings (
                id         INTEGER  PRIMARY KEY CHECK (id = 1),
                latitude   REAL,
                longitude  REAL,
                name       TEXT,
                time_zone  TEXT,
                updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            )
            """);
        try { db.NonQuery("ALTER TABLE home_settings ADD COLUMN units TEXT NULL"); }
        catch (DbException ex) when (ex.Message.Contains("duplicate column name", StringComparison.OrdinalIgnoreCase))
        {
            // added by an earlier start
        }
    }

    /// <summary>The saved settings, or null if none have been saved yet.</summary>
    public HomeSettings? Get() =>
        db.QueryOne("SELECT latitude, longitude, name, time_zone, units FROM home_settings WHERE id = 1",
            r => new HomeSettings(
                r.IsDBNull(0) ? null : r.GetDouble(0),
                r.IsDBNull(1) ? null : r.GetDouble(1),
                r.IsDBNull(2) ? null : r.GetString(2),
                r.IsDBNull(3) ? null : r.GetString(3),
                r.IsDBNull(4) ? null : r.GetString(4)));

    public HomeSettings Save(HomeSettings settings)
    {
        db.NonQuery("""
            INSERT INTO home_settings (id, latitude, longitude, name, time_zone, units, updated_at)
            VALUES (1, $latitude, $longitude, $name, $time_zone, $units, CURRENT_TIMESTAMP)
            ON CONFLICT (id) DO UPDATE SET
                latitude = excluded.latitude, longitude = excluded.longitude, name = excluded.name,
                time_zone = excluded.time_zone, units = excluded.units, updated_at = excluded.updated_at
            """, cmd =>
        {
            cmd.AddParam("$latitude", settings.Latitude);
            cmd.AddParam("$longitude", settings.Longitude);
            cmd.AddParam("$name", string.IsNullOrWhiteSpace(settings.Name) ? null : settings.Name.Trim());
            cmd.AddParam("$time_zone", string.IsNullOrWhiteSpace(settings.TimeZone) ? null : settings.TimeZone);
            cmd.AddParam("$units", settings.Units);
        });
        return Get()!;
    }
}
