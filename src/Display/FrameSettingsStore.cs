using System.Data.Common;
using System.Text.Json;
using Data.Abstractions;
using Display.Records;

namespace Display;

/// <summary>
/// Persists the single shared <see cref="FrameSettings"/> record (one row, id = 1).
/// </summary>
public sealed class FrameSettingsStore([FromKeyedServices("display")] IDatabase db)
{
    public void Migrate()
    {
        db.NonQuery("""
            CREATE TABLE IF NOT EXISTS frame_settings (
                id          INTEGER  PRIMARY KEY CHECK (id = 1),
                theme       TEXT     NOT NULL,
                font        TEXT     NOT NULL,
                orientation TEXT     NOT NULL,
                left_ids    TEXT     NOT NULL,
                right_ids   TEXT     NOT NULL,
                split       INTEGER  NOT NULL,
                updated_at  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            )
            """);

        foreach (var col in new[]
        {
            "ALTER TABLE frame_settings ADD COLUMN ambient_position TEXT NOT NULL DEFAULT 'bottom-left'",
            "ALTER TABLE frame_settings ADD COLUMN photo_source TEXT NOT NULL DEFAULT 'unsplash'",
            "ALTER TABLE frame_settings ADD COLUMN photo_categories TEXT NOT NULL DEFAULT '[]'",
        })
        {
            try { db.NonQuery(col); }
            catch (DbException ex) when (ex.Message.Contains("duplicate column name", StringComparison.OrdinalIgnoreCase))
            {
                // column already exists from a previous run
            }
        }
    }

    /// <summary>The saved settings, or null if none have been saved yet.</summary>
    public FrameSettings? Get() =>
        db.QueryOne("""
            SELECT theme, font, orientation, left_ids, right_ids, split, ambient_position, photo_source, photo_categories
            FROM frame_settings WHERE id = 1
            """, r => new FrameSettings(
                r.GetString(0),
                r.GetString(1),
                r.GetString(2),
                JsonSerializer.Deserialize<string[]>(r.GetString(3)) ?? [],
                JsonSerializer.Deserialize<string[]>(r.GetString(4)) ?? [],
                r.GetInt32(5),
                r.GetString(6),
                r.GetString(7),
                JsonSerializer.Deserialize<string[]>(r.GetString(8)) ?? []));

    public FrameSettings Save(FrameSettings settings)
    {
        db.NonQuery("""
            INSERT INTO frame_settings (id, theme, font, orientation, left_ids, right_ids, split, ambient_position,
                                        photo_source, photo_categories, updated_at)
            VALUES (1, $theme, $font, $orientation, $left, $right, $split, $ambient_position,
                    $photo_source, $photo_categories, CURRENT_TIMESTAMP)
            ON CONFLICT (id) DO UPDATE SET
                theme = excluded.theme, font = excluded.font, orientation = excluded.orientation,
                left_ids = excluded.left_ids, right_ids = excluded.right_ids, split = excluded.split,
                ambient_position = excluded.ambient_position, photo_source = excluded.photo_source,
                photo_categories = excluded.photo_categories, updated_at = excluded.updated_at
            """, cmd =>
        {
            cmd.AddParam("$theme", settings.Theme);
            cmd.AddParam("$font", settings.Font);
            cmd.AddParam("$orientation", settings.Orientation);
            cmd.AddParam("$left", JsonSerializer.Serialize(settings.Left));
            cmd.AddParam("$right", JsonSerializer.Serialize(settings.Right));
            cmd.AddParam("$split", settings.Split);
            cmd.AddParam("$ambient_position", settings.AmbientPosition);
            cmd.AddParam("$photo_source", settings.PhotoSource);
            cmd.AddParam("$photo_categories", JsonSerializer.Serialize(settings.PhotoCategories ?? []));
        });
        return Get()!;
    }
}
