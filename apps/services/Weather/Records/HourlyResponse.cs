namespace Weather.Records;

/// <param name="Time">Open-Meteo's local ISO hour, "2026-06-28T14:00".</param>
/// <param name="Temperature">In the current conditions' <c>TemperatureUnit</c>.</param>
/// <param name="PrecipitationChance">The hour's chance of precipitation, in percent.</param>
public record HourlyResponse(
    string Time,
    double Temperature,
    int WeatherCode,
    int? PrecipitationChance = null
);
