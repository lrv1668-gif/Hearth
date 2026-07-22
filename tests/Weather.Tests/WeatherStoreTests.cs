using Weather.Tests.Helpers;
using Xunit;

namespace Weather.Tests;

public sealed class WeatherStoreTests
{
    private static WeatherStore Migrated(TempDatabase tmp)
    {
        var store = new WeatherStore(tmp.Db);
        store.Migrate();
        return store;
    }

    [Fact]
    public void SaveThenLoad_RoundTripsCachedJson()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        store.Save("""{"temp":72}""", """[{"date":"2026-06-28"}]""");

        var cache = store.Load();
        Assert.NotNull(cache);
        Assert.Equal("""{"temp":72}""", cache!.CurrentJson);
        Assert.Equal("""[{"date":"2026-06-28"}]""", cache.ForecastJson);
    }

    [Fact]
    public void Load_BeforeAnySave_ReturnsNull()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        Assert.Null(store.Load());
    }

    [Fact]
    public void Save_CalledTwice_ReplacesSingleRow()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        store.Save("""{"v":1}""", "[]");
        store.Save("""{"v":2}""", "[]");

        var cache = store.Load();
        Assert.NotNull(cache);
        Assert.Equal("""{"v":2}""", cache!.CurrentJson);
    }

    [Fact]
    public void IsStale_FreshCache_ReturnsFalse()
    {
        var cache = new WeatherCache("{}", "[]", DateTime.UtcNow.ToString("o"));
        Assert.False(WeatherStore.IsStale(cache));
    }

    [Fact]
    public void IsStale_CacheOlderThanThirtyMinutes_ReturnsTrue()
    {
        var cache = new WeatherCache("{}", "[]", DateTime.UtcNow.AddMinutes(-31).ToString("o"));
        Assert.True(WeatherStore.IsStale(cache));
    }

    [Fact]
    public void IsStale_UnparseableTimestamp_ReturnsTrue()
    {
        var cache = new WeatherCache("{}", "[]", "not-a-date");
        Assert.True(WeatherStore.IsStale(cache));
    }
}
