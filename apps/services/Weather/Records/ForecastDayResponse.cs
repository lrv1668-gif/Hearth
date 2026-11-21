namespace Weather.Records;

public record ForecastDayResponse(
    string Date,
    int WeatherCode,
    string Description,
    double TempMaxF,
    double TempMinF,
    string Sunrise,
    string Sunset,
    double? UvIndexMax = null,
    int? PrecipitationChance = null
)
{
    public string? UvMaxCategory => WeatherCategories.Uv(UvIndexMax);
}
