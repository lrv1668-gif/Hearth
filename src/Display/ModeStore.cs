using System.Globalization;
using System.Text.Json;
using Data.Abstractions;
using Display.Records;
using ServiceDefaults;

namespace Display;

/// <summary>
/// Persists the frame's mode schedule and the current override — one row each, kept
/// apart from frame_settings, which is saved whole on every Settings change.
/// </summary>
public sealed class ModeStore([FromKeyedServices("display")] IDatabase db)
{
    private static readonly JsonSerializerOptions Json = HearthJson.SnakeCaseLower;

    public void Migrate()
    {
        db.NonQuery("""
            CREATE TABLE IF NOT EXISTS mode_schedule (
                id           INTEGER  PRIMARY KEY CHECK (id = 1),
                default_mode TEXT     NOT NULL,
                rules        TEXT     NOT NULL,
                updated_at   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            )
            """);
        db.NonQuery("""
            CREATE TABLE IF NOT EXISTS mode_override (
                id         INTEGER  PRIMARY KEY CHECK (id = 1),
                mode       TEXT     NOT NULL,
                until      TEXT,
                photo_id   TEXT,
                updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            )
            """);
    }

    /// <summary>The saved schedule, or <see cref="ModeSchedule.Default"/> if none is saved.</summary>
    public ModeSchedule GetSchedule() =>
        db.QueryOne("SELECT default_mode, rules FROM mode_schedule WHERE id = 1", r => new ModeSchedule(
            r.GetString(0),
            JsonSerializer.Deserialize<ModeRule[]>(r.GetString(1), Json) ?? []))
        ?? ModeSchedule.Default;

    public ModeSchedule SaveSchedule(ModeSchedule schedule)
    {
        db.NonQuery("""
            INSERT INTO mode_schedule (id, default_mode, rules, updated_at)
            VALUES (1, $default_mode, $rules, CURRENT_TIMESTAMP)
            ON CONFLICT (id) DO UPDATE SET
                default_mode = excluded.default_mode, rules = excluded.rules, updated_at = excluded.updated_at
            """, cmd =>
        {
            cmd.AddParam("$default_mode", schedule.DefaultMode);
            cmd.AddParam("$rules", JsonSerializer.Serialize(schedule.Rules, Json));
        });
        return GetSchedule();
    }

    /// <summary>The saved override, expired or not; null if none is set.</summary>
    public ModeOverride? GetOverride() =>
        db.QueryOne("SELECT mode, until, photo_id FROM mode_override WHERE id = 1", r => new ModeOverride(
            r.GetString(0),
            r.IsDBNull(1) ? null : DateTimeOffset.Parse(r.GetString(1), CultureInfo.InvariantCulture),
            r.IsDBNull(2) ? null : r.GetString(2)));

    public ModeOverride SetOverride(ModeOverride manual)
    {
        db.NonQuery("""
            INSERT INTO mode_override (id, mode, until, photo_id, updated_at)
            VALUES (1, $mode, $until, $photo_id, CURRENT_TIMESTAMP)
            ON CONFLICT (id) DO UPDATE SET
                mode = excluded.mode, until = excluded.until, photo_id = excluded.photo_id,
                updated_at = excluded.updated_at
            """, cmd =>
        {
            cmd.AddParam("$mode", manual.Mode);
            cmd.AddParam("$until", manual.Until?.ToUniversalTime().ToString("O"));
            cmd.AddParam("$photo_id", manual.PhotoId);
        });
        return GetOverride()!;
    }

    public void ClearOverride() => db.NonQuery("DELETE FROM mode_override WHERE id = 1");
}
