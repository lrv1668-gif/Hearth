namespace Weather.Records;

/// <param name="TempMax">The day's high, in the current conditions' <c>TemperatureUnit</c>.</param>
/// <param name="TempMin">The day's low, likewise.</param>
/// <param name="WindSpeedMax">The day's strongest sustained wind, in the current conditions' <c>WindUnit</c>.</param>
/// <param name="PrecipitationSum">The day's expected rain, showers and melted snow, in the current conditions' <c>PrecipitationUnit</c>.</param>
/// <param name="SnowfallSum">The day's expected snow depth, in the current conditions' <c>SnowfallUnit</c>.</param>
/// <param name="Expected">Those in words, "0.3 in" or "2 in snow" (<see cref="WeatherAmounts.Expected"/>); null when there's too little to say.</param>
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
    double? WindSpeedMax = null,
    double? PrecipitationSum = null,
    double? SnowfallSum = null,
    string? Expected = null
)
{
    public string? UvMaxCategory => WeatherCategories.Uv(UvIndexMax);
}
