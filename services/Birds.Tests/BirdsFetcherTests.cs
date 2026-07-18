using System.Net;
using Birds.Records;
using Birds.Tests.Helpers;
using Xunit;

namespace Birds.Tests;

public sealed class BirdsFetcherTests
{
    private static BirdObservation MakeObs(
        string species = "cangoo",
        string observedAt = "2026-07-17 08:15",
        double lat = 40.0,
        double lng = -75.0) =>
        new(species, $"Common {species}", $"Scientificus {species}", "Local Park", observedAt, 1, lat, lng);

    private static BirdsFetcher MakeFetcher(string recentJson, string notableJson = "[]",
        HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new FakeHttpMessageHandler(new Dictionary<string, string>
        {
            ["recent"] = recentJson,
            ["recent/notable"] = notableJson,
        }, status);
        return new BirdsFetcher(new HttpClient(handler));
    }

    private const string SingleObservationJson = """
        [{"speciesCode":"carwre","comName":"Carolina Wren","sciName":"Thryothorus ludovicianus",
          "locName":"Backyard Feeder","obsDt":"2026-07-16 07:30","howMany":2,
          "lat":40.0,"lng":-75.0,"obsValid":true,"obsReviewed":false,"locationPrivate":false}]
        """;

    [Fact]
    public async Task FetchAsync_ValidResponse_MapsAllFields()
    {
        var fetcher = MakeFetcher(SingleObservationJson);

        var result = await fetcher.FetchAsync("key", 40.0, -75.0, 15);

        var sighting = Assert.Single(result);
        Assert.Equal("carwre", sighting.SpeciesCode);
        Assert.Equal("Carolina Wren", sighting.CommonName);
        Assert.Equal("Thryothorus ludovicianus", sighting.ScientificName);
        Assert.Equal("Backyard Feeder", sighting.Location);
        Assert.Equal("2026-07-16 07:30", sighting.ObservedAt);
        Assert.Equal(2, sighting.Count);
        Assert.Equal(0, sighting.DistanceMi);
        Assert.False(sighting.IsNotable);
    }

    [Fact]
    public async Task FetchAsync_MissingHowMany_MapsNullCount()
    {
        var fetcher = MakeFetcher("""
            [{"speciesCode":"carwre","comName":"Carolina Wren","sciName":"Thryothorus ludovicianus",
              "locName":"Backyard Feeder","obsDt":"2026-07-16 07:30","lat":40.0,"lng":-75.0}]
            """);

        var result = await fetcher.FetchAsync("key", 40.0, -75.0, 15);

        Assert.Null(Assert.Single(result).Count);
    }

    [Fact]
    public async Task FetchAsync_HttpError_Throws()
    {
        var fetcher = MakeFetcher("[]", "[]", HttpStatusCode.ServiceUnavailable);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => fetcher.FetchAsync("key", 40.0, -75.0, 15));
    }

    [Fact]
    public void Merge_SpeciesInNotableList_FlaggedNotable()
    {
        var recent = new List<BirdObservation> { MakeObs("cangoo"), MakeObs("pifgoo") };
        var notable = new List<BirdObservation> { MakeObs("pifgoo") };

        var result = BirdsFetcher.Merge(recent, notable, 40.0, -75.0);

        Assert.True(result.Single(s => s.SpeciesCode == "pifgoo").IsNotable);
        Assert.False(result.Single(s => s.SpeciesCode == "cangoo").IsNotable);
    }

    [Fact]
    public void Merge_DuplicateSpecies_KeepsMostRecentObservation()
    {
        var recent = new List<BirdObservation>
        {
            MakeObs("cangoo", observedAt: "2026-07-15 09:00"),
            MakeObs("cangoo", observedAt: "2026-07-16 18:45"),
        };

        var result = BirdsFetcher.Merge(recent, [], 40.0, -75.0);

        var sighting = Assert.Single(result);
        Assert.Equal("2026-07-16 18:45", sighting.ObservedAt);
    }

    [Fact]
    public void Merge_NotableOnlySpecies_IncludedInResult()
    {
        var notable = new List<BirdObservation> { MakeObs("rarbir") };

        var result = BirdsFetcher.Merge([], notable, 40.0, -75.0);

        var sighting = Assert.Single(result);
        Assert.Equal("rarbir", sighting.SpeciesCode);
        Assert.True(sighting.IsNotable);
    }

    [Fact]
    public void Merge_ManySpecies_SortsNewestFirstAndCapsAtTwelve()
    {
        var recent = Enumerable.Range(1, 20)
            .Select(i => MakeObs($"sp{i:D2}", observedAt: $"2026-07-{i:D2} 12:00"))
            .ToList();

        var result = BirdsFetcher.Merge(recent, [], 40.0, -75.0);

        Assert.Equal(12, result.Count);
        Assert.Equal("sp20", result.First().SpeciesCode);
        Assert.Equal(
            result.Select(s => s.ObservedAt).OrderByDescending(t => t).ToList(),
            result.Select(s => s.ObservedAt).ToList());
    }

    [Fact]
    public void Merge_DistantObservation_ComputesHaversineDistance()
    {
        // One degree of latitude is ~69.1 miles
        var recent = new List<BirdObservation> { MakeObs(lat: 41.0, lng: -75.0) };

        var result = BirdsFetcher.Merge(recent, [], 40.0, -75.0);

        Assert.InRange(Assert.Single(result).DistanceMi, 68.5, 69.5);
    }
}
