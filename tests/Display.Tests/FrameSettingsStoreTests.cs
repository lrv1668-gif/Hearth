using Display;
using Display.Records;
using Hearth.TestUtilities;
using Xunit;

namespace Display.Tests;

public sealed class FrameSettingsStoreTests
{
    private static readonly FrameSettings Sample =
        new("almanac", "inter", "landscape", ["countdowns", "today"], ["coming-up"], 50);

    private static FrameSettingsStore Migrated(TempDatabase tmp)
    {
        var store = new FrameSettingsStore(tmp.Db);
        store.Migrate();
        return store;
    }

    [Fact]
    public void Get_NothingSaved_ReturnsNull()
    {
        using var tmp = new TempDatabase();
        Assert.Null(Migrated(tmp).Get());
    }

    [Fact]
    public void Save_ThenGet_RoundTripsEveryField()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        store.Save(Sample);

        var saved = store.Get()!;
        Assert.Equal("almanac", saved.Theme);
        Assert.Equal("inter", saved.Font);
        Assert.Equal("landscape", saved.Orientation);
        Assert.Equal(["countdowns", "today"], saved.Left);
        Assert.Equal(["coming-up"], saved.Right);
        Assert.Equal(50, saved.Split);
        Assert.Equal("bottom-left", saved.AmbientPosition);
        Assert.Equal("unsplash", saved.PhotoSource);
        Assert.Empty(saved.PhotoCategories!);
        Assert.Equal((1600, 1200), (saved.Width, saved.Height));
        Assert.Empty(saved.PhotoIds!);
        Assert.Equal("full", saved.PhotoCard);
        Assert.Null(saved.CountdownId);
        Assert.Equal(FrameSettings.DefaultPhotoIntervalMinutes, saved.PhotoIntervalMinutes);
        Assert.Equal(FrameSettings.DefaultPortraitRotation, saved.PortraitRotation);
        Assert.Equal(FrameSettings.DefaultTextSize, saved.TextSize);
        Assert.Equal(FrameSettings.DefaultDithering, saved.Dithering);
    }

    [Fact]
    public void Migrate_RowFromBeforePanelRenderChoices_ReadsTheirDefaults()
    {
        using var tmp = new TempDatabase();
        tmp.Db.NonQuery("""
            CREATE TABLE frame_settings (
                id INTEGER PRIMARY KEY CHECK (id = 1), theme TEXT NOT NULL, font TEXT NOT NULL,
                orientation TEXT NOT NULL, left_ids TEXT NOT NULL, right_ids TEXT NOT NULL,
                split INTEGER NOT NULL, updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP)
            """);
        tmp.Db.NonQuery("""
            INSERT INTO frame_settings (id, theme, font, orientation, left_ids, right_ids, split)
            VALUES (1, 'almanac', 'inter', 'portrait', '[]', '[]', 50)
            """);

        var saved = Migrated(tmp).Get()!;

        Assert.Equal(FrameSettings.DefaultPhotoIntervalMinutes, saved.PhotoIntervalMinutes);
        Assert.Equal(FrameSettings.DefaultPortraitRotation, saved.PortraitRotation);
        Assert.Equal(FrameSettings.DefaultTextSize, saved.TextSize);
        Assert.Equal(FrameSettings.DefaultDithering, saved.Dithering);
    }

    [Fact]
    public void Save_PanelRenderChoices_RoundTrip()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        store.Save(Sample with { PhotoIntervalMinutes = 180, PortraitRotation = 270, TextSize = 125, Dithering = "frame" });

        var saved = store.Get()!;
        Assert.Equal(180, saved.PhotoIntervalMinutes);
        Assert.Equal(270, saved.PortraitRotation);
        Assert.Equal(125, saved.TextSize);
        Assert.Equal("frame", saved.Dithering);
    }

    [Fact]
    public void Save_PhotoAndCountdownChoices_RoundTrip()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        store.Save(Sample with { PhotoIds = ["a1", "b2"], PhotoCard = "none", CountdownId = 7 });

        var saved = store.Get()!;
        Assert.Equal(["a1", "b2"], saved.PhotoIds);
        Assert.Equal("none", saved.PhotoCard);
        Assert.Equal(7, saved.CountdownId);
    }

    [Fact]
    public void Save_Note_RoundTripsWithItsLineBreaks()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        store.Save(Sample with { NoteTitle = "House rules", NoteBody = "Shoes off\n\nHave fun!" });

        var saved = store.Get()!;
        Assert.Equal("House rules", saved.NoteTitle);
        Assert.Equal("Shoes off\n\nHave fun!", saved.NoteBody);
    }

    [Fact]
    public void Save_BlankNote_StoresNull()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        store.Save(Sample with { NoteTitle = "  ", NoteBody = "" });

        Assert.Null(store.Get()!.NoteTitle);
        Assert.Null(store.Get()!.NoteBody);
    }

    [Fact]
    public void Save_PanelSize_RoundTrips()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        store.Save(Sample with { Width = 800, Height = 480 });

        var saved = store.Get()!;
        Assert.Equal((800, 480), (saved.Width, saved.Height));
    }

    [Fact]
    public void Save_PhotoSourceAndCategories_RoundTrip()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        store.Save(Sample with { PhotoSource = "both", PhotoCategories = ["nature", "architecture"] });

        var saved = store.Get()!;
        Assert.Equal("both", saved.PhotoSource);
        Assert.Equal(["nature", "architecture"], saved.PhotoCategories);
    }

    [Fact]
    public void Save_AmbientPosition_RoundTrips()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        store.Save(Sample with { AmbientPosition = "bottom-middle" });

        Assert.Equal("bottom-middle", store.Get()!.AmbientPosition);
    }

    [Fact]
    public void Save_Twice_KeepsOneRecordWithTheLatestValues()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        store.Save(Sample);
        var latest = store.Save(Sample with { Theme = "night", Right = [], Split = 60 });

        Assert.Equal("night", latest.Theme);
        Assert.Empty(latest.Right);
        Assert.Equal(60, store.Get()!.Split);
    }

    [Fact]
    public void Migrate_RunTwice_KeepsSavedSettings()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);
        store.Save(Sample);

        store.Migrate();

        Assert.Equal("almanac", store.Get()!.Theme);
    }
}
