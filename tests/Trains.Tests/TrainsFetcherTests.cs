using System.Net;
using Trains.Tests.Helpers;
using Xunit;

namespace Trains.Tests;

public sealed class TrainsFetcherTests
{
    private static TrainsFetcher MakeFetcher(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(new HttpClient(new FakeHttpMessageHandler(json, status)));

    [Theory]
    [InlineData(3, "bus")]
    [InlineData(2, "rail")]
    [InlineData(1, "subway")]
    [InlineData(0, "tram")]
    public async Task FetchAsync_KnownRouteType_MapsToMode(int routeType, string expectedMode)
    {
        const string template = """
            {"stops": [{"stop_name": "Union Station", "departures": [
                {"stop_headsign": "Downtown", "departure_time": "14:32:00",
                 "schedule_relationship": "SCHEDULED",
                 "trip": {"route": {"route_type": ROUTE_TYPE, "route_short_name": "15", "route_long_name": "Federal Blvd"}}}
            ]}]}
            """;
        var fetcher = MakeFetcher(template.Replace("ROUTE_TYPE", routeType.ToString()));

        var result = await fetcher.FetchAsync("key", "stop-key");

        var departure = Assert.Single(result.Departures);
        Assert.Equal(expectedMode, departure.Mode);
        Assert.Equal(routeType, departure.RouteType);
        Assert.Equal("15", departure.RouteShortName);
        Assert.Equal("Federal Blvd", departure.RouteLongName);
        Assert.Equal("Downtown", departure.Headsign);
    }

    [Fact]
    public async Task FetchAsync_StaticScheduleRelationship_NotRealtimeAndNoEstimate()
    {
        var fetcher = MakeFetcher("""
            {"stops": [{"stop_name": "Union Station", "departures": [
                {"departure_time": "14:32:00", "schedule_relationship": "STATIC",
                 "trip": {"route": {"route_type": 3, "route_short_name": "15"}}}
            ]}]}
            """);

        var result = await fetcher.FetchAsync("key", "stop-key");

        var departure = Assert.Single(result.Departures);
        Assert.Equal("14:32:00", departure.ScheduledDeparture);
        Assert.Null(departure.EstimatedDeparture);
        Assert.False(departure.IsRealtime);
    }

    [Fact]
    public async Task FetchAsync_ScheduledRelationshipWithEstimate_MapsEstimatedTime()
    {
        var fetcher = MakeFetcher("""
            {"stops": [{"stop_name": "Union Station", "departures": [
                {"departure_time": "14:32:00", "schedule_relationship": "SCHEDULED",
                 "departure": {"estimated": "14:35:00"},
                 "trip": {"route": {"route_type": 3, "route_short_name": "15"}}}
            ]}]}
            """);

        var result = await fetcher.FetchAsync("key", "stop-key");

        var departure = Assert.Single(result.Departures);
        Assert.True(departure.IsRealtime);
        Assert.Equal("14:35:00", departure.EstimatedDeparture);
    }

    [Fact]
    public async Task FetchAsync_MissingRouteInfo_DefaultsSafely()
    {
        var fetcher = MakeFetcher("""
            {"stops": [{"stop_name": "Union Station", "departures": [
                {"departure_time": "14:32:00"}
            ]}]}
            """);

        var result = await fetcher.FetchAsync("key", "stop-key");

        var departure = Assert.Single(result.Departures);
        Assert.Equal(-1, departure.RouteType);
        Assert.Equal("transit", departure.Mode);
        Assert.Equal("", departure.RouteShortName);
        Assert.Null(departure.RouteLongName);
    }

    [Fact]
    public async Task FetchAsync_ParentStationWithNoOwnDepartures_MergesChildStopDepartures()
    {
        // A parent station (location_type 1) carries no departures of its own — real trips are
        // attached to its child stops (individual platforms/gates).
        var fetcher = MakeFetcher("""
            {"stops": [{"stop_name": "Union Station", "departures": [], "children": [
                {"stop_name": "Gate B4", "departures": [
                    {"departure_time": "09:34:30", "schedule_relationship": "SCHEDULED",
                     "trip": {"route": {"route_type": 3, "route_short_name": "15"}}}
                ]},
                {"stop_name": "Gate B6", "departures": [
                    {"departure_time": "09:41:30", "schedule_relationship": "SCHEDULED",
                     "trip": {"route": {"route_type": 0, "route_short_name": "W"}}}
                ]}
            ]}]}
            """);

        var result = await fetcher.FetchAsync("key", "stop-key");

        Assert.Equal(2, result.Departures.Count());
        Assert.Contains(result.Departures, d => d.RouteShortName == "15" && d.Mode == "bus");
        Assert.Contains(result.Departures, d => d.RouteShortName == "W" && d.Mode == "tram");
    }

    [Fact]
    public async Task FetchAsync_StopWithOwnDeparturesAndNoChildren_ReturnsOwnDeparturesOnly()
    {
        var fetcher = MakeFetcher("""
            {"stops": [{"stop_name": "Track 12", "departures": [
                {"departure_time": "09:34:30", "schedule_relationship": "SCHEDULED",
                 "trip": {"route": {"route_type": 0, "route_short_name": "C"}}}
            ]}]}
            """);

        var result = await fetcher.FetchAsync("key", "stop-key");

        Assert.Single(result.Departures);
    }

    [Fact]
    public async Task FetchAsync_EmptyStops_ReturnsEmptyDepartures()
    {
        var fetcher = MakeFetcher("""{"stops": []}""");

        var result = await fetcher.FetchAsync("key", "stop-key");

        Assert.Equal("stop-key", result.StopKey);
        Assert.Null(result.StopName);
        Assert.Empty(result.Departures);
    }

    [Fact]
    public async Task FetchAsync_HttpError_Throws()
    {
        var fetcher = MakeFetcher("""{"error": "bad key"}""", HttpStatusCode.Unauthorized);

        await Assert.ThrowsAsync<HttpRequestException>(() => fetcher.FetchAsync("key", "stop-key"));
    }
}
