namespace Weather.Records;

public record CurrentWeatherResponse(
    double TemperatureF,
    int WeatherCode,
    string Description,
    double WindMph,
    string FetchedAt
);
