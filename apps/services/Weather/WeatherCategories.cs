namespace Weather;

/// <summary>Plain-language bands for the numeric readings, so every screen words them the same way.</summary>
public static class WeatherCategories
{
    /// <summary>WHO UV index bands, on the index rounded the way screens show it (2.5 → 3).</summary>
    public static string? Uv(double? index) => index is not { } value ? null : Math.Round(value, MidpointRounding.AwayFromZero) switch
    {
        <= 2  => "Low",
        <= 5  => "Moderate",
        <= 7  => "High",
        <= 10 => "Very high",
        _     => "Extreme",
    };

    /// <summary>EPA US AQI bands.</summary>
    public static string? Aqi(int? aqi) => aqi switch
    {
        null   => null,
        <= 50  => "Good",
        <= 100 => "Moderate",
        <= 150 => "Unhealthy for sensitive groups",
        <= 200 => "Unhealthy",
        <= 300 => "Very unhealthy",
        _      => "Hazardous",
    };
}
