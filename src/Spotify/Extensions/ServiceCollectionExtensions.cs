using Data.Extensions;
using ServiceDefaults;
using Spotify;

namespace Spotify.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForSpotify(this IServiceCollection services)
    {
        services.AddSqliteDatabase("spotify", "spotify.db");
        services.AddHearthDataProtection("spotify");
        services.AddSingleton<SpotifyStore>();
        services.AddSingleton<SpotifyClientService>();

        services.AddHearthWebDefaults();
    }
}
