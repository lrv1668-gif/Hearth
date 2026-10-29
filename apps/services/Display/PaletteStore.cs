using Data.Abstractions;
using Display.Records;

namespace Display;

/// <summary>Persists the frame's <see cref="FramePalette"/> (one row, id = 1).</summary>
public sealed class PaletteStore([FromKeyedServices("display")] IDatabase db)
{
    public void Migrate()
    {
        db.NonQuery("""
            CREATE TABLE IF NOT EXISTS frame_palette (
                id         INTEGER  PRIMARY KEY CHECK (id = 1),
                palette    TEXT     NOT NULL,
                updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            )
            """);
    }

    /// <summary>The saved palette, or <see cref="FramePalette.Default"/> if none is saved.</summary>
    public FramePalette Get() =>
        db.QueryOne("SELECT palette FROM frame_palette WHERE id = 1", r => new FramePalette(r.GetString(0)))
        ?? FramePalette.Default;

    public FramePalette Save(FramePalette palette)
    {
        db.NonQuery("""
            INSERT INTO frame_palette (id, palette, updated_at)
            VALUES (1, $palette, CURRENT_TIMESTAMP)
            ON CONFLICT (id) DO UPDATE SET palette = excluded.palette, updated_at = excluded.updated_at
            """, cmd => cmd.AddParam("$palette", palette.Palette));
        return Get();
    }
}
