using System.Globalization;
using System.Net;

namespace Display;

/// <summary>
/// Calls the renderer sidecar (apps/renderer/server.mjs), which screenshots the /frame route
/// in headless Chromium and dithers it to the panel's six inks.
/// </summary>
public sealed class RendererClient(HttpClient http)
{
    /// <summary>
    /// A dithered /frame capture at width×height as PNG, or throws
    /// <see cref="HttpRequestException"/> carrying the renderer's status code.
    /// </summary>
    /// <param name="dither">"ordered", "errorDiffusion" or "none".</param>
    /// <param name="kernel">epdoptimize's error-diffusion kernel; null for its default.</param>
    /// <param name="look">"screen" (the inks the panel is sent) or "paper" (how they look on it).</param>
    /// <param name="timeZone">IANA zone the page's clock runs in; null for the renderer's TZ.</param>
    /// <param name="rotate">Clockwise degrees to turn the result (0, 90, 180 or 270) — portrait onto the panel.</param>
    public async Task<byte[]> RenderAsync(
        string framePath, int width, int height, string dither, string? kernel, string look,
        string? timeZone = null, int rotate = 0, CancellationToken ct = default)
    {
        var url = string.Create(CultureInfo.InvariantCulture,
            $"render?path={Uri.EscapeDataString(framePath)}&width={width}&height={height}&dither={dither}&look={look}");
        if (kernel is not null) url += $"&kernel={Uri.EscapeDataString(kernel)}";
        if (timeZone is not null) url += $"&tz={Uri.EscapeDataString(timeZone)}";
        if (rotate != 0) url += string.Create(CultureInfo.InvariantCulture, $"&rotate={rotate}");

        using var response = await http.GetAsync(url, ct);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var detail = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"renderer: {detail}", null, response.StatusCode);
        }
        return await response.Content.ReadAsByteArrayAsync(ct);
    }
}
