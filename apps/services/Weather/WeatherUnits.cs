namespace Weather;

/// <summary>The units a snapshot is fetched in: the household's choice in Settings → Home.</summary>
public static class WeatherUnits
{
    public const string Fahrenheit = "F";
    public const string Celsius    = "C";
    public const string Mph        = "mph";
    public const string Kmh        = "km/h";

    /// <summary>Open-Meteo's query parameters for the units.</summary>
    public static string Query(bool metric) =>
        metric ? "&temperature_unit=celsius&wind_speed_unit=kmh" : "&temperature_unit=fahrenheit&wind_speed_unit=mph";
}
