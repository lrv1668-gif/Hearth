using System.Globalization;
using System.Text.Json;
using Display.Records;

namespace Display;

/// <summary>
/// City search for Settings → Home, via Open-Meteo's keyless geocoding API (Weather
/// already uses Open-Meteo). Each result carries its time zone.
/// </summary>
public sealed class GeocodingClient(HttpClient http)
{
    public const int MaxResults = 6;

    public async Task<List<Place>> SearchAsync(string query, CancellationToken ct = default)
    {
        var url = string.Create(CultureInfo.InvariantCulture,
            $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(query)}&count={MaxResults}&language=en&format=json");
        using var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        // No matches: the response has no "results" at all
        if (!doc.RootElement.TryGetProperty("results", out var results)) return [];

        return [.. results.EnumerateArray().Select(r =>
        {
            var name = r.GetProperty("name").GetString() ?? "";
            var parts = new[] { name, Text(r, "admin1"), Text(r, "country") }
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct();
            return new Place(
                name,
                string.Join(", ", parts),
                r.GetProperty("latitude").GetDouble(),
                r.GetProperty("longitude").GetDouble(),
                Text(r, "timezone"));
        })];
    }

    private static string? Text(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
