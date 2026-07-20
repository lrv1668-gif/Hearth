namespace Almanac.Extensions;

public static class WebApplicationExtensions
{
    public static void InitializeWebAppForAlmanac(this WebApplication app)
    {
        var config = app.Services.GetRequiredService<IConfiguration>();
        if (string.IsNullOrEmpty(config["LATITUDE"]) || string.IsNullOrEmpty(config["LONGITUDE"]))
        {
            // Unlike Weather, missing coordinates are not fatal: season and phenology
            // are date-only, so /almanac still returns 200 with daylight omitted.
            app.Logger.LogError(
                "LATITUDE and LONGITUDE are not set — the daylight section will be omitted. Update the .env file to add your coordinates.");
        }

        app.AddAlmanacEndpoints();
    }

    private static void AddAlmanacEndpoints(this WebApplication app)
    {
        app.MapGet("/almanac", (AlmanacService service) =>
            Results.Ok(service.Build(DateTimeOffset.UtcNow)));
    }
}
