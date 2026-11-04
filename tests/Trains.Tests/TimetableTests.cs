using System.Net;
using Hearth.TestUtilities;
using Trains.Records;

namespace Trains.Tests;

public sealed class TimetableTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 19, 20, 0, TimeSpan.Zero);

    private static TrainDeparture At(string route, string? headsign, string? utc, string mode = "rail") =>
        new(route, null, 2, mode, headsign, "19:00:00", null, false, utc);

    [Fact]
    public void Build_KeepsTheHourAndTheNext_GroupedByLineAndDirection()
    {
        var stop = new StopDepartures("s-1", "Fruitvale",
        [
            At("Red", "Richmond", "2026-11-04T19:28:00Z"),
            At("Red", "Richmond", "2026-11-04T19:16:00Z"),
            At("Red", "Millbrae", "2026-11-04T19:05:00Z"),
            At("Red", "Richmond", "2026-11-04T21:00:00Z"), // past the window
            At("Red", "Richmond", "2026-11-04T18:59:00Z"), // before the hour
        ]);

        var timetable = Timetable.Build(stop, Now);

        Assert.Equal("2026-11-04T19:00:00Z", timetable.WindowStart);
        Assert.Equal("2026-11-04T21:00:00Z", timetable.WindowEnd);
        Assert.Collection(timetable.Lines,
            line =>
            {
                Assert.Equal("Millbrae", line.Headsign);
                Assert.Equal(["2026-11-04T19:05:00Z"], line.Departures);
            },
            line =>
            {
                Assert.Equal("Richmond", line.Headsign);
                Assert.Equal(["2026-11-04T19:16:00Z", "2026-11-04T19:28:00Z"], line.Departures);
            });
    }

    [Fact]
    public void Build_SameTripOnTwoPlatformsAndMissingInstants_ListedOnceOrDropped()
    {
        var stop = new StopDepartures("s-1", null,
        [
            At("1", null, "2026-11-04T19:30:00Z", "bus"),
            At("1", null, "2026-11-04T19:30:00Z", "bus"),
            At("1", null, null, "bus"),
        ]);

        var line = Assert.Single(Timetable.Build(stop, Now).Lines);

        Assert.Null(line.Headsign);
        Assert.Equal(["2026-11-04T19:30:00Z"], line.Departures);
    }

    [Fact]
    public void NextSeconds_CoversNowToTheEndOfTheNextHour()
    {
        Assert.Equal(100 * 60, Timetable.NextSeconds(Now));
    }

    [Fact]
    public async Task FetchAsync_WithWindow_AsksForItAndReadsScheduledUtc()
    {
        var handler = new FakeHttpMessageHandler("""
            {"stops": [{"stop_name": "Fruitvale", "departures": [
                {"departure_time": "11:05:00", "schedule_relationship": "STATIC",
                 "departure": {"scheduled": "11:05:00", "scheduled_utc": "2026-11-04T19:05:00Z", "estimated": null},
                 "trip": {"trip_headsign": "Richmond", "route": {"route_type": 1, "route_short_name": "Red"}}}
            ]}]}
            """);
        var fetcher = new TrainsFetcher(new HttpClient(handler));

        var result = await fetcher.FetchAsync("key", "stop-key", nextSeconds: 6000, limit: 200);

        Assert.Contains("&next=6000&limit=200", handler.LastRequestUri?.Query);
        Assert.Equal("2026-11-04T19:05:00Z", Assert.Single(result.Departures).ScheduledUtc);
    }
}
