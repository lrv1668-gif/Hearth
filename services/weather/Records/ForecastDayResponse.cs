namespace Weather.Records;

public record ForecastDayResponse(
    string Date,
    int WeatherCode,
    string Description,
    double TempMaxF,
    double TempMinF
);
