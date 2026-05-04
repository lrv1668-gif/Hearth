using System.Text.Json;
using Weather.Records;

namespace Weather;

public sealed class WeatherFetcher(HttpClient http)
{
    private static readonly Dictionary<int, string> WmoDescriptions = new()
    {
        [0]  = "Clear sky",
        [1]  = "Mainly clear", [2]  = "Partly cloudy", [3]  = "Overcast",
        [45] = "Fog",          [48] = "Icy fog",
        [51] = "Light drizzle", [53] = "Drizzle",       [55] = "Heavy drizzle",
        [56] = "Freezing drizzle", [57] = "Heavy freezing drizzle",
        [61] = "Light rain",   [63] = "Rain",            [65] = "Heavy rain",
        [66] = "Freezing rain", [67] = "Heavy freezing rain",
        [71] = "Light snow",   [73] = "Snow",            [75] = "Heavy snow",
        [77] = "Snow grains",
        [80] = "Light showers", [81] = "Showers",        [82] = "Heavy showers",
        [85] = "Snow showers",  [86] = "Heavy snow showers",
        [95] = "Thunderstorm",
        [96] = "Thunderstorm with hail", [99] = "Thunderstorm with heavy hail",
    };

    private static string Describe(int code) =>
        WmoDescriptions.TryGetValue(code, out var desc) ? desc : "Unknown";

    public async Task<(CurrentWeatherResponse Current, List<ForecastDayResponse> Forecast)> FetchAsync(
        double latitude, double longitude)
    {
        var url = $"https://api.open-meteo.com/v1/forecast" +
                  $"?latitude={latitude}&longitude={longitude}" +
                  $"&current=temperature_2m,weather_code,wind_speed_10m" +
                  $"&daily=temperature_2m_max,temperature_2m_min,weather_code,sunrise,sunset" +
                  $"&temperature_unit=fahrenheit&wind_speed_unit=mph" +
                  $"&timezone=auto&forecast_days=7";
        
        using var response = await http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        var current = root.GetProperty("current");
        var tempF   = current.GetProperty("temperature_2m").GetDouble();
        var wCode   = current.GetProperty("weather_code").GetInt32();
        var windMph = current.GetProperty("wind_speed_10m").GetDouble();
        var fetchedAt = DateTime.UtcNow.ToString("o");

        var currentResponse = new CurrentWeatherResponse(tempF, wCode, Describe(wCode), windMph, fetchedAt);

        var daily       = root.GetProperty("daily");
        var dates       = daily.GetProperty("time");
        var codes       = daily.GetProperty("weather_code");
        var maxTemps    = daily.GetProperty("temperature_2m_max");
        var minTemps    = daily.GetProperty("temperature_2m_min");
        var sunrises    = daily.GetProperty("sunrise");
        var sunsets     = daily.GetProperty("sunset");

        var forecast = new List<ForecastDayResponse>();
        for (int i = 0; i < dates.GetArrayLength(); i++)
        {
            var code = codes[i].GetInt32();
            forecast.Add(new ForecastDayResponse(
                dates[i].GetString()!,
                code,
                Describe(code),
                maxTemps[i].GetDouble(),
                minTemps[i].GetDouble(),
                sunrises[i].GetString()!,
                sunsets[i].GetString()!));
        }

        return (currentResponse, forecast);
    }
}
