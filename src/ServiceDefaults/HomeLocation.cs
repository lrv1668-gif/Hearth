using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;

namespace ServiceDefaults;

/// <summary>
/// Where the household is: the location and zone saved in Settings, else the service's
/// LATITUDE / LONGITUDE / TZ env vars. Either part can be missing.
/// </summary>
public sealed record HomeLocation(double? Latitude, double? Longitude, string? TimeZone)
{
    public (double Latitude, double Longitude)? Coordinates =>
        Latitude is { } lat && Longitude is { } lon ? (lat, lon) : null;
}

/// <summary>
/// Reads the shared <see cref="HomeSettings"/> from the Display service (DISPLAY_URL) and
/// merges in the env-var fallbacks, so installs configured through .env keep working.
/// </summary>
public sealed class HomeLocationClient(
    HttpClient http, IFusionCache cache, IConfiguration config, ILogger<HomeLocationClient> logger)
{
    private const string CacheKey = "home-settings";

    // Short, so a change saved in Settings shows up within a minute; fail-safe keeps the
    // last answer while Display restarts.
    private static readonly FusionCacheEntryOptions CacheOptions = new()
    {
        Duration = TimeSpan.FromMinutes(1),
        IsFailSafeEnabled = true,
        FailSafeMaxDuration = TimeSpan.FromDays(7),
    };

    public async Task<HomeLocation> GetAsync(CancellationToken ct = default)
    {
        var stored = await GetStoredAsync(ct);
        var coordinates = stored is { Latitude: { } lat, Longitude: { } lon }
            ? (lat, lon)
            : EnvCoordinates();
        return new HomeLocation(
            coordinates?.Item1,
            coordinates?.Item2,
            stored?.TimeZone ?? (string.IsNullOrWhiteSpace(config["TZ"]) ? null : config["TZ"]));
    }

    private (double, double)? EnvCoordinates() =>
        Parse(config["LATITUDE"]) is { } lat && Parse(config["LONGITUDE"]) is { } lon ? (lat, lon) : null;

    private static double? Parse(string? value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null;

    private async Task<HomeSettings?> GetStoredAsync(CancellationToken ct)
    {
        if (http.BaseAddress is null) return null; // no DISPLAY_URL: env vars only
        try
        {
            return await cache.GetOrSetAsync<HomeSettings?>(CacheKey, async (_, token) =>
            {
                using var response = await http.GetAsync("display/home", token);
                response.EnsureSuccessStatusCode();
                return response.StatusCode == HttpStatusCode.NoContent
                    ? null
                    : await response.Content.ReadFromJsonAsync<HomeSettings>(HearthJson.SnakeCaseLower, token);
            }, CacheOptions, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            // Never reached Display (and nothing cached): carry on with the env vars
            logger.LogWarning(ex, "Couldn't read the home location from the Display service — using env vars");
            return null;
        }
    }
}

public static class HomeLocationServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="HomeLocationClient"/>. Needs <see cref="HearthCache.AddHearthCache"/>.
    /// </summary>
    public static IServiceCollection AddHearthHomeLocation(this IServiceCollection services)
    {
        services.AddHttpClient<HomeLocationClient>((sp, http) =>
        {
            if (sp.GetRequiredService<IConfiguration>()["DISPLAY_URL"] is { Length: > 0 } url)
                http.BaseAddress = new Uri(url.EndsWith('/') ? url : url + "/");
            http.Timeout = TimeSpan.FromSeconds(3);
        });
        return services;
    }
}
