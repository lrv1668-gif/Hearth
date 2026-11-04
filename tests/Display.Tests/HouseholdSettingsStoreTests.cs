using Display;
using Display.Records;
using Hearth.TestUtilities;

namespace Display.Tests;

public sealed class HouseholdSettingsStoreTests : IDisposable
{
    private readonly TempDatabase _db = new();

    public void Dispose() => _db.Dispose();

    private HouseholdSettingsStore Migrated()
    {
        var store = new HouseholdSettingsStore(_db.Db);
        store.Migrate();
        return store;
    }

    [Fact]
    public void Get_NothingSaved_ReturnsNull()
    {
        Assert.Null(Migrated().Get());
    }

    [Fact]
    public void Save_ThenGet_RoundTripsFeedsStopsAndLineFilters()
    {
        var store = Migrated();

        var saved = store.Save(new HouseholdSettings(
            [new RssFeedSetting("BBC", "https://feeds.bbci.co.uk/news/rss.xml")],
            [new TrainStopSetting("Home", "s-9q9p-home", ["Red|Richmond|rail", "1|Downtown|bus"])]));

        Assert.Equal("BBC", Assert.Single(saved.RssFeeds).Title);
        var stop = Assert.Single(store.Get()!.TrainStops);
        Assert.Equal("s-9q9p-home", stop.StopKey);
        Assert.Equal(["Red|Richmond|rail", "1|Downtown|bus"], stop.LineFilter);
    }

    [Fact]
    public void Save_EmptyLineFilter_KeptApartFromNoFilter()
    {
        var saved = Migrated().Save(new HouseholdSettings([],
            [new TrainStopSetting("Hidden", "s-1", []), new TrainStopSetting("All", "s-2")]));

        Assert.Empty(saved.TrainStops[0].LineFilter!);
        Assert.Null(saved.TrainStops[1].LineFilter);
    }

    [Fact]
    public void Save_Twice_KeepsOneRowWithTheLatest()
    {
        var store = Migrated();
        store.Save(new HouseholdSettings([new RssFeedSetting("A", "https://a.example/rss")], []));

        var saved = store.Save(new HouseholdSettings([], []));

        Assert.Empty(saved.RssFeeds);
    }

    [Fact]
    public void Migrate_RunTwice_KeepsData()
    {
        Migrated().Save(new HouseholdSettings([new RssFeedSetting("A", "https://a.example/rss")], []));

        Assert.Single(Migrated().Get()!.RssFeeds);
    }
}
