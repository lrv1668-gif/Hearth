using Display;
using Hearth.TestUtilities;
using ServiceDefaults;
using Xunit;

namespace Display.Tests;

public sealed class HomeSettingsStoreTests
{
    private static HomeSettingsStore Migrated(TempDatabase tmp)
    {
        var store = new HomeSettingsStore(tmp.Db);
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
        var home = new HomeSettings(39.7392, -104.9903, "Denver, Colorado", "America/Denver");

        store.Save(home);

        Assert.Equal(home, store.Get());
    }

    [Fact]
    public void Save_Twice_KeepsOneRowWithTheLatest()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);
        store.Save(new HomeSettings(39.7, -104.9, "Denver", "America/Denver"));

        var saved = store.Save(new HomeSettings(null, null, null, "Europe/London"));

        Assert.Equal(new HomeSettings(null, null, null, "Europe/London"), saved);
    }

    [Fact]
    public void Save_BlankNameAndZone_StoredAsNull()
    {
        using var tmp = new TempDatabase();

        var saved = Migrated(tmp).Save(new HomeSettings(1, 2, "  ", ""));

        Assert.Null(saved.Name);
        Assert.Null(saved.TimeZone);
    }

    [Fact]
    public void Migrate_RunTwice_KeepsData()
    {
        using var tmp = new TempDatabase();
        Migrated(tmp).Save(new HomeSettings(1, 2, null, null));

        Assert.NotNull(Migrated(tmp).Get());
    }
}
