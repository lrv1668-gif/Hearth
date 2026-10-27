using ServiceDefaults;

namespace Almanac.Extensions;

public static class WebApplicationExtensions
{
    private static int _reportedMissing;

    public static void InitializeWebAppForAlmanac(this WebApplication app)
    {
        app.AddAlmanacEndpoints();
        app.MapHearthHealth("almanac");
    }

    private static void AddAlmanacEndpoints(this WebApplication app)
    {
        app.MapGet("/almanac", async (AlmanacService service, HomeLocationClient homeClient, CancellationToken ct) =>
        {
            var home = await homeClient.GetAsync(ct);
            // Unlike Weather, missing coordinates are not fatal: season and phenology
            // are date-only, so /almanac still returns 200 with daylight omitted.
            // Logged once per change rather than on every poll
            var missing = home.Coordinates is null;
            if (missing && Interlocked.Exchange(ref _reportedMissing, 1) == 0)
                app.Logger.LogError("No home location — the daylight section will be omitted. Set one in Settings → Home, or LATITUDE and LONGITUDE in the .env file.");
            else if (!missing) Interlocked.Exchange(ref _reportedMissing, 0);
            return Results.Ok(service.Build(DateTimeOffset.UtcNow, home));
        });
    }
}
