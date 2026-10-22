using ServiceDefaults;

namespace Almanac.Extensions;

public static class WebApplicationExtensions
{
    public static void InitializeWebAppForAlmanac(this WebApplication app)
    {
        app.AddAlmanacEndpoints();
    }

    private static void AddAlmanacEndpoints(this WebApplication app)
    {
        app.MapGet("/almanac", async (AlmanacService service, HomeLocationClient homeClient, CancellationToken ct) =>
        {
            var home = await homeClient.GetAsync(ct);
            // Unlike Weather, missing coordinates are not fatal: season and phenology
            // are date-only, so /almanac still returns 200 with daylight omitted.
            if (home.Coordinates is null)
                app.Logger.LogError("No home location — the daylight section will be omitted. Set one in Settings → Home, or LATITUDE and LONGITUDE in the .env file.");
            return Results.Ok(service.Build(DateTimeOffset.UtcNow, home));
        });
    }
}
