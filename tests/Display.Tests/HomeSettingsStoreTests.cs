using Display;
using Hearth.TestUtilities;
using ServiceDefaults;

namespace Display.Tests;

public sealed class HomeSettingsStoreTests : IDisposable
{
    private readonly TempDatabase _db = new();

    public void Dispose() => _db.Dispose();

    private HomeSettingsStore Migrated()
    {
        var store = new HomeSettingsStore(_db.Db);
        store.Migrate();
        return store;
    }

    [Fact]
    public void Get_NothingSaved_ReturnsNull()
    {
        Assert.Null(Migrated().Get());
    }

    [Fact]
    public void Save_ThenGet_RoundTripsEveryField()
    {
        var store = Migrated();
        var home = new HomeSettings(39.7392, -104.9903, "Denver, Colorado", "America/Denver");

        store.Save(home);

        Assert.Equal(home, store.Get());
    }

    [Fact]
    public void Save_Twice_KeepsOneRowWithTheLatest()
    {
        var store = Migrated();
        store.Save(new HomeSettings(39.7, -104.9, "Denver", "America/Denver"));

        var saved = store.Save(new HomeSettings(null, null, null, "Europe/London"));

        Assert.Equal(new HomeSettings(null, null, null, "Europe/London"), saved);
    }

    [Fact]
    public void Save_BlankNameAndZone_StoredAsNull()
    {

        var saved = Migrated().Save(new HomeSettings(1, 2, "  ", ""));

        Assert.Null(saved.Name);
        Assert.Null(saved.TimeZone);
    }

    [Fact]
    public void Migrate_RunTwice_KeepsData()
    {
        Migrated().Save(new HomeSettings(1, 2, null, null));

        Assert.NotNull(Migrated().Get());
    }
}
