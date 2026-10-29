using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Display.Records;
using ServiceDefaults;

namespace Display;

/// <summary>
/// Calls the Photos service (PHOTOS_URL) for the frame's Ambient and Photo pictures.
/// Throws <see cref="HttpRequestException"/> when it can't be reached.
/// </summary>
public sealed class PhotosClient(HttpClient http)
{
    /// <summary>False without PHOTOS_URL — /frame then picks its own photo.</summary>
    public bool Configured => http.BaseAddress is not null;

    /// <summary>A random photo from <paramref name="source"/>, or null when it has none.</summary>
    /// <param name="exclude">Ids to skip while others are left, most recently shown last.</param>
    public async Task<FramePhoto?> RandomAsync(
        string source, string query, string orientation, IReadOnlyList<string>? exclude = null, CancellationToken ct = default)
    {
        var url = string.Create(CultureInfo.InvariantCulture,
            $"photos/random?source={Uri.EscapeDataString(source)}&query={Uri.EscapeDataString(query)}&orientation={orientation}");
        if (exclude is { Count: > 0 }) url += "&exclude=" + Uri.EscapeDataString(string.Join(',', exclude));
        using var response = await http.GetAsync(url, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FramePhoto>(HearthJson.SnakeCaseLower, ct);
    }

    public async Task<IReadOnlyList<UploadedPhoto>> UploadsAsync(CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<UploadedPhoto>>("photos/uploads", HearthJson.SnakeCaseLower, ct) ?? [];
}
