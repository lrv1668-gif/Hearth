using System.Text.Json;
using Photos.Records;

namespace Photos;

public sealed class PhotoFetcher(HttpClient http)
{
    public async Task<List<PhotoResponse>> FetchAsync(string query, string orientation, string apiKey)
    {
        var url = $"https://api.unsplash.com/photos/random?query={Uri.EscapeDataString(query)}&orientation={orientation}&count=20";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"Client-ID {apiKey}");

        using var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var results = new List<PhotoResponse>();
        foreach (var photo in doc.RootElement.EnumerateArray())
        {
            var id           = photo.GetProperty("id").GetString()!;
            var imageUrl     = photo.GetProperty("urls").GetProperty("regular").GetString()!;
            var photographer = photo.GetProperty("user").GetProperty("name").GetString()!;
            var link         = photo.GetProperty("links").GetProperty("html").GetString()!;

            string? desc = null;
            if (photo.TryGetProperty("description", out var d) && d.ValueKind == JsonValueKind.String)
                desc = d.GetString();
            else if (photo.TryGetProperty("alt_description", out var alt) && alt.ValueKind == JsonValueKind.String)
                desc = alt.GetString();

            results.Add(new PhotoResponse(id, imageUrl, null, desc, photographer, link, "unsplash"));
        }

        return results;
    }
}
