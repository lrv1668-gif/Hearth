using ServiceDefaults;

namespace Display;

public static class HomeZones
{
    /// <summary>The household's zone from Settings → Home, else TZ, else the host's.</summary>
    public static TimeZoneInfo Resolve(HomeSettings? home, IConfiguration config)
    {
        var id = home?.TimeZone ?? config["TZ"];
        return !string.IsNullOrWhiteSpace(id) && TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone)
            ? zone
            : TimeZoneInfo.Local;
    }
}
