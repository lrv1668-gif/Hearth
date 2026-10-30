using System.Text.Json;
using Display;
using Display.Records;
using Hearth.TestUtilities;
using ServiceDefaults;

namespace Display.Tests;

public sealed class FrameSettingsStoreTests : IDisposable
{
    private static readonly FrameSettings Sample = TestData.Settings with { Left = ["countdowns", "today"], Right = ["coming-up"] };

    /// <summary>Every field away from its default, so a column the store forgets shows up.</summary>
    private static readonly FrameSettings Everything = new(
        "night", "lora", "portrait", ["countdowns", "today"], ["coming-up"], 60,
        AmbientPosition: "bottom-middle", PhotoSource: "both", PhotoCategories: ["nature", "architecture"],
        Width: 800, Height: 480, PhotoIds: ["a1", "b2"], PhotoCard: "none", CountdownId: 7,
        NoteTitle: "House rules", NoteBody: "Shoes off\n\nHave fun!", PhotoIntervalMinutes: 180,
        PortraitRotation: 270, TextSize: 125, Dithering: "frame", QuietStart: "23:00", QuietEnd: "06:30",
        Template: "feature", Slots: ["week", "", "quote"]);

    private readonly TempDatabase _db = new();

    public void Dispose() => _db.Dispose();

    private FrameSettingsStore Migrated()
    {
        var store = new FrameSettingsStore(_db.Db);
        store.Migrate();
        return store;
    }

    // Records compare their lists by reference, so compare what the API would send
    private static string Json(FrameSettings? settings) => JsonSerializer.Serialize(settings, HearthJson.SnakeCaseLower);

    [Fact]
    public void Get_NothingSaved_ReturnsNull()
    {
        Assert.Null(Migrated().Get());
    }

    [Fact]
    public void Save_EveryFieldSet_RoundTrips()
    {
        var store = Migrated();

        store.Save(Everything);

        Assert.Equal(Json(Everything), Json(store.Get()));
    }

    [Fact]
    public void Save_OnlyTheRequiredFields_ReadsBackTheDefaults()
    {
        var store = Migrated();

        store.Save(Sample);

        var saved = store.Get()!;
        Assert.Equal(Json(Sample with { PhotoCategories = [], PhotoIds = [], Slots = [] }), Json(saved));
    }

    [Fact]
    public void Migrate_RowFromBeforePanelRenderChoices_ReadsTheirDefaults()
    {
        _db.Db.NonQuery("""
            CREATE TABLE frame_settings (
                id INTEGER PRIMARY KEY CHECK (id = 1), theme TEXT NOT NULL, font TEXT NOT NULL,
                orientation TEXT NOT NULL, left_ids TEXT NOT NULL, right_ids TEXT NOT NULL,
                split INTEGER NOT NULL, updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP)
            """);
        _db.Db.NonQuery("""
            INSERT INTO frame_settings (id, theme, font, orientation, left_ids, right_ids, split)
            VALUES (1, 'almanac', 'inter', 'portrait', '[]', '[]', 50)
            """);

        var saved = Migrated().Get()!;

        Assert.Equal(FrameSettings.DefaultPhotoIntervalMinutes, saved.PhotoIntervalMinutes);
        Assert.Equal(FrameSettings.DefaultPortraitRotation, saved.PortraitRotation);
        Assert.Equal(FrameSettings.DefaultTextSize, saved.TextSize);
        Assert.Equal(FrameSettings.DefaultDithering, saved.Dithering);
        Assert.Equal(FrameSettings.DefaultTemplate, saved.Template);
        Assert.Empty(saved.Slots!);
    }

    [Fact]
    public void Save_BlankNoteAndNoQuietHours_StoresNull()
    {
        var store = Migrated();
        store.Save(Everything);

        store.Save(Everything with { NoteTitle = "  ", NoteBody = "", QuietStart = null, QuietEnd = null });

        var saved = store.Get()!;
        Assert.Null(saved.NoteTitle);
        Assert.Null(saved.NoteBody);
        Assert.Null(saved.QuietStart);
    }

    [Fact]
    public void Save_Twice_KeepsOneRecordWithTheLatestValues()
    {
        var store = Migrated();

        store.Save(Sample);
        var latest = store.Save(Sample with { Theme = "night", Right = [], Split = 60 });

        Assert.Equal("night", latest.Theme);
        Assert.Empty(latest.Right);
        Assert.Equal(60, store.Get()!.Split);
    }

    [Fact]
    public void Migrate_RunTwice_KeepsSavedSettings()
    {
        var store = Migrated();
        store.Save(Sample);

        store.Migrate();

        Assert.Equal("almanac", store.Get()!.Theme);
    }
}
