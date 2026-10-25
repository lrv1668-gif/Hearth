namespace Weather.Records;

public record CurrentWeatherResponse(
    double TemperatureF,
    int WeatherCode,
    string Description,
    double WindMph,
    string FetchedAt,
    double? UvIndex = null,
    int? UsAqi = null
)
{
    // Derived, not stored: a cached snapshot's category always matches its value.
    public string? UvCategory  => WeatherCategories.Uv(UvIndex);
    public string? AqiCategory => WeatherCategories.Aqi(UsAqi);
}
