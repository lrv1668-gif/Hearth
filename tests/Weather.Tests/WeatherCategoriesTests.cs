using System.Text.Json;
using ServiceDefaults;
using Weather.Records;

namespace Weather.Tests;

public sealed class WeatherCategoriesTests
{
    [Theory]
    [InlineData(0.0,  "Low")]
    [InlineData(2.4,  "Low")]
    [InlineData(2.5,  "Moderate")]
    [InlineData(5.0,  "Moderate")]
    [InlineData(6.0,  "High")]
    [InlineData(7.0,  "High")]
    [InlineData(8.0,  "Very high")]
    [InlineData(10.0, "Very high")]
    [InlineData(11.0, "Extreme")]
    public void Uv_BandEdges_ReturnsWhoCategory(double index, string expected)
    {
        Assert.Equal(expected, WeatherCategories.Uv(index));
    }

    [Theory]
    [InlineData(0,   "Good")]
    [InlineData(50,  "Good")]
    [InlineData(51,  "Moderate")]
    [InlineData(100, "Moderate")]
    [InlineData(101, "Unhealthy for sensitive groups")]
    [InlineData(150, "Unhealthy for sensitive groups")]
    [InlineData(151, "Unhealthy")]
    [InlineData(200, "Unhealthy")]
    [InlineData(201, "Very unhealthy")]
    [InlineData(300, "Very unhealthy")]
    [InlineData(301, "Hazardous")]
    public void Aqi_BandEdges_ReturnsEpaCategory(int aqi, string expected)
    {
        Assert.Equal(expected, WeatherCategories.Aqi(aqi));
    }

    [Fact]
    public void Categories_NullReadings_ReturnNull()
    {
        Assert.Null(WeatherCategories.Uv(null));
        Assert.Null(WeatherCategories.Aqi(null));
    }

    [Fact]
    public void Snapshot_SerializedForCache_WritesCategoriesAndRoundTrips()
    {
        var snapshot = new WeatherSnapshot(
            new CurrentWeatherResponse(70, 0, "Clear sky", 5, "2026-10-24T12:00:00Z", 6.4, 42),
            [new ForecastDayResponse("2026-10-24", 0, "Clear sky", 75, 55, "07:00", "18:00", 4.1)]);

        var json = JsonSerializer.Serialize(snapshot, HearthCache.SerializerOptions);
        var back = JsonSerializer.Deserialize<WeatherSnapshot>(json, HearthCache.SerializerOptions)!;

        Assert.Contains("\"aqi_category\":\"Good\"", json);
        Assert.Contains("\"uv_max_category\":\"Moderate\"", json);
        Assert.Equal("High", back.Current.UvCategory);
        Assert.Equal(4.1, back.Forecast[0].UvIndexMax);
        Assert.Equal("Moderate", back.Forecast[0].UvMaxCategory);
    }
}
