namespace Weather.Records;

/// <param name="TempMax">The day's high, in the current conditions' <c>TemperatureUnit</c>.</param>
/// <param name="TempMin">The day's low, likewise.</param>
/// <param name="WindSpeedMax">The day's strongest sustained wind, in the current conditions' <c>WindUnit</c>.</param>
public record ForecastDayResponse(
    string Date,
    int WeatherCode,
    string Description,
    double TempMax,
    double TempMin,
    string Sunrise,
    string Sunset,
    double? UvIndexMax = null,
    int? PrecipitationChance = null,
    double? WindSpeedMax = null
)
{
    public string? UvMaxCategory => WeatherCategories.Uv(UvIndexMax);
}
