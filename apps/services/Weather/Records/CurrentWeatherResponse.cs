namespace Weather.Records;

/// <param name="Temperature">In <paramref name="TemperatureUnit"/> ("F" or "C").</param>
/// <param name="WindSpeed">In <paramref name="WindUnit"/> ("mph" or "km/h").</param>
/// <param name="ApparentTemperature">Feels-like, in <paramref name="TemperatureUnit"/>.</param>
/// <param name="Humidity">Relative humidity, in percent.</param>
/// <param name="WindDirection">Where the wind blows from, in degrees.</param>
public record CurrentWeatherResponse(
    double Temperature,
    int WeatherCode,
    string Description,
    double WindSpeed,
    string FetchedAt,
    double? UvIndex = null,
    int? UsAqi = null,
    string TemperatureUnit = WeatherUnits.Fahrenheit,
    string WindUnit = WeatherUnits.Mph,
    double? ApparentTemperature = null,
    int? Humidity = null,
    double? WindDirection = null
)
{
    // Derived, not stored: a cached snapshot's category always matches its value.
    public string? UvCategory  => WeatherCategories.Uv(UvIndex);
    public string? AqiCategory => WeatherCategories.Aqi(UsAqi);
    public string? WindCompass => WeatherCategories.Compass(WindDirection);
}
