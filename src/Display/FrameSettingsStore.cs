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
            "ALTER TABLE frame_settings ADD COLUMN width INTEGER NOT NULL DEFAULT 1600",
            "ALTER TABLE frame_settings ADD COLUMN height INTEGER NOT NULL DEFAULT 1200",
            "ALTER TABLE frame_settings ADD COLUMN photo_ids TEXT NOT NULL DEFAULT '[]'",
            "ALTER TABLE frame_settings ADD COLUMN photo_card TEXT NOT NULL DEFAULT 'full'",
            "ALTER TABLE frame_settings ADD COLUMN countdown_id INTEGER",
            "ALTER TABLE frame_settings ADD COLUMN note_title TEXT",
            "ALTER TABLE frame_settings ADD COLUMN note_body TEXT",
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
            SELECT theme, font, orientation, left_ids, right_ids, split, ambient_position, photo_source, photo_categories,
                   width, height, photo_ids, photo_card, countdown_id, note_title, note_body
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
                JsonSerializer.Deserialize<string[]>(r.GetString(8)) ?? [],
                r.GetInt32(9),
                r.GetInt32(10),
                JsonSerializer.Deserialize<string[]>(r.GetString(11)) ?? [],
                r.GetString(12),
                r.IsDBNull(13) ? null : r.GetInt32(13),
                r.IsDBNull(14) ? null : r.GetString(14),
                r.IsDBNull(15) ? null : r.GetString(15)));

    public FrameSettings Save(FrameSettings settings)
    {
        db.NonQuery("""
            INSERT INTO frame_settings (id, theme, font, orientation, left_ids, right_ids, split, ambient_position,
                                        photo_source, photo_categories, width, height, photo_ids, photo_card,
                                        countdown_id, note_title, note_body, updated_at)
            VALUES (1, $theme, $font, $orientation, $left, $right, $split, $ambient_position,
                    $photo_source, $photo_categories, $width, $height, $photo_ids, $photo_card,
                    $countdown_id, $note_title, $note_body, CURRENT_TIMESTAMP)
            ON CONFLICT (id) DO UPDATE SET
                theme = excluded.theme, font = excluded.font, orientation = excluded.orientation,
                left_ids = excluded.left_ids, right_ids = excluded.right_ids, split = excluded.split,
                ambient_position = excluded.ambient_position, photo_source = excluded.photo_source,
                photo_categories = excluded.photo_categories, width = excluded.width,
                height = excluded.height, photo_ids = excluded.photo_ids, photo_card = excluded.photo_card,
                countdown_id = excluded.countdown_id, note_title = excluded.note_title,
                note_body = excluded.note_body, updated_at = excluded.updated_at
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
            cmd.AddParam("$width", settings.Width);
            cmd.AddParam("$height", settings.Height);
            cmd.AddParam("$photo_ids", JsonSerializer.Serialize(settings.PhotoIds ?? []));
            cmd.AddParam("$photo_card", settings.PhotoCard);
            cmd.AddParam("$countdown_id", settings.CountdownId);
            cmd.AddParam("$note_title", string.IsNullOrWhiteSpace(settings.NoteTitle) ? null : settings.NoteTitle);
            cmd.AddParam("$note_body", string.IsNullOrWhiteSpace(settings.NoteBody) ? null : settings.NoteBody);
        });
        return Get()!;
    }
}
