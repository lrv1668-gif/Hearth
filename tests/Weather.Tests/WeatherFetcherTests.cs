using System.Globalization;
using System.Net;
using System.Text.Json;
using Hearth.TestUtilities;

namespace Weather.Tests;

public sealed class WeatherFetcherTests
{
    private const string ValidJson = """
    {
      "current": { "temperature_2m": 72.5, "weather_code": 0, "wind_speed_10m": 5.0, "uv_index": 6.4 },
      "daily": {
        "time": ["2026-06-28", "2026-06-29"],
        "temperature_2m_max": [80.1, 78.0],
        "temperature_2m_min": [60.0, 58.5],
        "weather_code": [0, 999],
        "sunrise": ["2026-06-28T05:30", "2026-06-29T05:31"],
        "sunset": ["2026-06-28T20:30", "2026-06-29T20:29"],
        "uv_index_max": [7.6, null]
      }
    }
    """;

    private const string ValidAqiJson = """{ "current": { "us_aqi": 42 } }""";

    private const string AirQualityPath = "/v1/air-quality";

    private static WeatherFetcher MakeFetcher(
        string json,
        HttpStatusCode status = HttpStatusCode.OK,
        string? airQualityJson = null,
        HttpStatusCode airQualityStatus = HttpStatusCode.OK) =>
        MakeFetcher(out _, json, status, airQualityJson, airQualityStatus);

    private static WeatherFetcher MakeFetcher(
        out FakeHttpMessageHandler handler,
        string json,
        HttpStatusCode status = HttpStatusCode.OK,
        string? airQualityJson = null,
        HttpStatusCode airQualityStatus = HttpStatusCode.OK)
    {
        handler = new FakeHttpMessageHandler(json, status);
        if (airQualityJson is not null)
            handler.ByPath[AirQualityPath] = (airQualityStatus, airQualityJson);
        return new WeatherFetcher(new HttpClient(handler));
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
    public async Task FetchAsync_ValidResponses_MapsUvIndexAndUsAqi()
    {
        var fetcher = MakeFetcher(ValidJson, airQualityJson: ValidAqiJson);

        var (current, _) = await fetcher.FetchAsync(40.0, -75.0);

        Assert.Equal(6.4, current.UvIndex);
        Assert.Equal("High", current.UvCategory);
        Assert.Equal(42, current.UsAqi);
        Assert.Equal("Good", current.AqiCategory);
    }

    [Fact]
    public async Task FetchAsync_DailyUvIndexMax_MapsPeakAndNullGaps()
    {
        var fetcher = MakeFetcher(ValidJson);

        var (_, forecast) = await fetcher.FetchAsync(40.0, -75.0);

        Assert.Equal(7.6, forecast[0].UvIndexMax);
        Assert.Equal("Very high", forecast[0].UvMaxCategory);
        Assert.Null(forecast[1].UvIndexMax);
        Assert.Null(forecast[1].UvMaxCategory);
    }

    [Fact]
    public async Task FetchAsync_DailyUvIndexMaxMissing_ReturnsNullPeak()
    {
        var noUvMaxJson = ValidJson.Replace("\"uv_index_max\"", "\"unused\"");
        var fetcher = MakeFetcher(noUvMaxJson);

        var (_, forecast) = await fetcher.FetchAsync(40.0, -75.0);

        Assert.All(forecast, day => Assert.Null(day.UvIndexMax));
    }

    [Fact]
    public async Task FetchAsync_CommaDecimalCulture_FormatsCoordinatesInvariantly()
    {
        var fetcher = MakeFetcher(out var handler, ValidJson, airQualityJson: ValidAqiJson);
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            await fetcher.FetchAsync(47.6, -122.3);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }

        Assert.All(handler.RequestUris, uri =>
            Assert.Contains("latitude=47.6&longitude=-122.3", uri.Query));
    }

    [Fact]
    public async Task FetchAsync_AirQualityUnavailable_ReturnsNullAqi()
    {
        var fetcher = MakeFetcher(ValidJson,
            airQualityJson: "", airQualityStatus: HttpStatusCode.ServiceUnavailable);

        var (current, _) = await fetcher.FetchAsync(40.0, -75.0);

        Assert.Equal(72.5, current.TemperatureF);
        Assert.Null(current.UsAqi);
    }

    [Fact]
    public async Task FetchAsync_AirQualityMalformedJson_ReturnsNullAqi()
    {
        var fetcher = MakeFetcher(ValidJson, airQualityJson: "not-json");

        var (current, _) = await fetcher.FetchAsync(40.0, -75.0);

        Assert.Null(current.UsAqi);
    }

    [Fact]
    public async Task FetchAsync_UvIndexMissing_ReturnsNullUvIndex()
    {
        var noUvJson = ValidJson.Replace(", \"uv_index\": 6.4", "");
        var fetcher = MakeFetcher(noUvJson, airQualityJson: ValidAqiJson);

        var (current, _) = await fetcher.FetchAsync(40.0, -75.0);

        Assert.Null(current.UvIndex);
        Assert.Equal(42, current.UsAqi);
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
