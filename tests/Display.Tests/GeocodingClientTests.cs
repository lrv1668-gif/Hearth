using Display;
using Hearth.TestUtilities;

namespace Display.Tests;

public sealed class GeocodingClientTests
{
    [Fact]
    public async Task SearchAsync_Results_MapsLabelCoordinatesAndZone()
    {
        var handler = new FakeHttpMessageHandler("""
            {"results":[{"name":"Denver","latitude":39.73915,"longitude":-104.9847,
                         "timezone":"America/Denver","country":"United States","admin1":"Colorado"}]}
            """);

        var places = await new GeocodingClient(new HttpClient(handler)).SearchAsync("Denver");

        var place = Assert.Single(places);
        Assert.Equal("Denver, Colorado, United States", place.Label);
        Assert.Equal(39.73915, place.Latitude);
        Assert.Equal("America/Denver", place.TimeZone);
        Assert.Contains("name=Denver", handler.LastRequestUri?.Query);
    }

    [Fact]
    public async Task SearchAsync_NoMatches_ReturnsEmpty()
    {
        var handler = new FakeHttpMessageHandler("""{"generationtime_ms":0.5}""");

        Assert.Empty(await new GeocodingClient(new HttpClient(handler)).SearchAsync("zzzz"));
    }
}
