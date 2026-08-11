using Trains.Tests.Helpers;
using Xunit;

namespace Trains.Tests;

public sealed class TrainsStoreTests
{
    private static TrainsStore Migrated(TempDatabase tmp)
    {
        var store = new TrainsStore(tmp.Db);
        store.Migrate();
        return store;
    }

    [Fact]
    public void IsStale_NoCachedStop_ReturnsTrue()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        Assert.True(store.IsStale("stop-a"));
    }

    [Fact]
    public void IsStale_FreshlySavedStop_ReturnsFalse()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        store.Save("stop-a", "{}");

        Assert.False(store.IsStale("stop-a"));
    }

    [Fact]
    public void IsStale_SavingOneStop_DoesNotFreshenAnotherStop()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        store.Save("stop-a", "{}");

        Assert.True(store.IsStale("stop-b"));
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsJson()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        store.Save("stop-a", """{"stop_key":"stop-a"}""");

        Assert.Equal("""{"stop_key":"stop-a"}""", store.Load("stop-a"));
    }

    [Fact]
    public void Load_UnknownStop_ReturnsNull()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        Assert.Null(store.Load("missing"));
    }

    [Fact]
    public void Save_CalledAgainForSameStop_ReplacesPreviousValue()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        store.Save("stop-a", "{\"v\":1}");
        store.Save("stop-a", "{\"v\":2}");

        Assert.Equal("{\"v\":2}", store.Load("stop-a"));
    }
}
