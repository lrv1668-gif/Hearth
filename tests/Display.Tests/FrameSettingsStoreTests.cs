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
