using System.Net;
using System.Text.Json;
using Weather.Tests.Helpers;
using Xunit;

namespace Weather.Tests;

public sealed class WeatherFetcherTests
{
    private const string ValidJson = """
    {
      "current": { "temperature_2m": 72.5, "weather_code": 0, "wind_speed_10m": 5.0 },
      "daily": {
        "time": ["2026-06-28", "2026-06-29"],
        "temperature_2m_max": [80.1, 78.0],
        "temperature_2m_min": [60.0, 58.5],
        "weather_code": [0, 999],
        "sunrise": ["2026-06-28T05:30", "2026-06-29T05:31"],
        "sunset": ["2026-06-28T20:30", "2026-06-29T20:29"]
      }
    }
    """;

    private static WeatherFetcher MakeFetcher(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new FakeHttpMessageHandler(json, status);
        var http = new HttpClient(handler);
        return new WeatherFetcher(http);
    }

    [Fact]
    public async Task FetchAsync_ValidResponse_MapsCurrentWeather()
    {
        var fetcher = MakeFetcher(ValidJson);

        var (current, _) = await fetcher.FetchAsync(40.0, -75.0);

        Assert.Equal(72.5, current.TemperatureF);
        Assert.Equal(0, current.WeatherCode);
        Assert.Equal("Clear sky", current.Description);
        Assert.Equal(5.0, current.WindMph);
    }

    [Fact]
    public async Task FetchAsync_ValidResponse_MapsSevenDayShapedForecast()
    {
        var fetcher = MakeFetcher(ValidJson);

        var (_, forecast) = await fetcher.FetchAsync(40.0, -75.0);

        Assert.Equal(2, forecast.Count);
        Assert.Equal("2026-06-28", forecast[0].Date);
        Assert.Equal(80.1, forecast[0].TempMaxF);
        Assert.Equal(60.0, forecast[0].TempMinF);
        Assert.Equal("2026-06-28T05:30", forecast[0].Sunrise);
        Assert.Equal("2026-06-28T20:30", forecast[0].Sunset);
    }

    [Fact]
    public async Task FetchAsync_UnknownWeatherCode_DescribesAsUnknown()
    {
        var fetcher = MakeFetcher(ValidJson);

        var (_, forecast) = await fetcher.FetchAsync(40.0, -75.0);

        // Second day uses WMO code 999, which is not in the description table.
        Assert.Equal("Unknown", forecast[1].Description);
    }

    [Fact]
    public async Task FetchAsync_HttpError_ThrowsHttpRequestException()
    {
        var fetcher = MakeFetcher("", HttpStatusCode.ServiceUnavailable);

        await Assert.ThrowsAsync<HttpRequestException>(() => fetcher.FetchAsync(40.0, -75.0));
    }

    [Fact]
    public async Task FetchAsync_MalformedJson_ThrowsJsonException()
    {
        var fetcher = MakeFetcher("not-json");

        // JsonDocument.Parse throws JsonReaderException, a subclass of JsonException.
        await Assert.ThrowsAnyAsync<JsonException>(() => fetcher.FetchAsync(40.0, -75.0));
    }
}
