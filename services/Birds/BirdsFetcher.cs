using System.Globalization;
using System.Text.Json;
using Birds.Records;

namespace Birds;

public sealed class BirdsFetcher(HttpClient http)
{
    private const int MaxSightings = 12;

    public async Task<List<BirdSighting>> FetchAsync(string apiKey, double latitude, double longitude, int radiusKm)
    {
        var recent = await FetchObservationsAsync(
            "https://api.ebird.org/v2/data/obs/geo/recent", apiKey, latitude, longitude, radiusKm);
        var notable = await FetchObservationsAsync(
            "https://api.ebird.org/v2/data/obs/geo/recent/notable", apiKey, latitude, longitude, radiusKm);

        return Merge(recent, notable, latitude, longitude);
    }

    /// <summary>
    /// Combines recent and notable observations: one sighting per species (most recent wins),
    /// notable species flagged, sorted newest first, capped at <see cref="MaxSightings"/>.
    /// </summary>
    public static List<BirdSighting> Merge(
        List<BirdObservation> recent, List<BirdObservation> notable, double homeLat, double homeLon)
    {
        var notableSpecies = notable.Select(o => o.SpeciesCode).ToHashSet();

        return recent.Concat(notable)
            .GroupBy(o => o.SpeciesCode)
            // "yyyy-MM-dd HH:mm" sorts chronologically as a string
            .Select(g => g.OrderByDescending(o => o.ObservedAt).First())
            .OrderByDescending(o => o.ObservedAt)
            .Take(MaxSightings)
            .Select(o => new BirdSighting(
                o.SpeciesCode,
                o.CommonName,
                o.ScientificName,
                o.Location,
                o.ObservedAt,
                o.Count,
                Math.Round(HaversineMiles(homeLat, homeLon, o.Latitude, o.Longitude), 1),
                notableSpecies.Contains(o.SpeciesCode)))
            .ToList();
    }

    private async Task<List<BirdObservation>> FetchObservationsAsync(
        string baseUrl, string apiKey, double latitude, double longitude, int radiusKm)
    {
        var url = string.Create(CultureInfo.InvariantCulture,
            $"{baseUrl}?lat={latitude:F4}&lng={longitude:F4}&dist={radiusKm}&back=7&maxResults=50");

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-eBirdApiToken", apiKey);

        using var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var observations = new List<BirdObservation>();
        foreach (var obs in doc.RootElement.EnumerateArray())
        {
            observations.Add(new BirdObservation(
                obs.GetProperty("speciesCode").GetString()!,
                obs.GetProperty("comName").GetString()!,
                obs.GetProperty("sciName").GetString()!,
                obs.TryGetProperty("locName", out var loc) ? loc.GetString() ?? "" : "",
                obs.GetProperty("obsDt").GetString()!,
                obs.TryGetProperty("howMany", out var n) ? n.GetInt32() : null,
                obs.GetProperty("lat").GetDouble(),
                obs.GetProperty("lng").GetDouble()));
        }

        return observations;
    }

    private static double HaversineMiles(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusMi = 3958.8;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return earthRadiusMi * 2 * Math.Asin(Math.Sqrt(a));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
