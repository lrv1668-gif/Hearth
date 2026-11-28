namespace Weather.Records;

/// <summary>Current conditions and forecast from one Open-Meteo fetch — cached together as one entry.</summary>
public record WeatherSnapshot(CurrentWeatherResponse Current, List<ForecastDayResponse> Forecast)
{
    /// <summary>The next 24 hours from the current one.</summary>
    public List<HourlyResponse> Hourly { get; init; } = [];
}
