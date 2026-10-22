using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ServiceDefaults;

namespace Almanac.Tests;

public class AlmanacServiceTests
{
    private static readonly DateTimeOffset MidJuly2026 = new(2026, 7, 19, 18, 0, 0, TimeSpan.Zero);

    private static readonly HomeLocation Denver = new(39.74, -104.99, "America/Denver");

    private static AlmanacService MakeService(Dictionary<string, string?>? values = null)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(values ?? []).Build();
        return new AlmanacService(config, NullLogger<AlmanacService>.Instance);
    }

    [Fact]
    public void Build_NoCoordinates_OmitsDaylightButKeepsSeasonAndNote()
    {
        var response = MakeService().Build(MidJuly2026, new HomeLocation(null, null, "UTC"));

        Assert.Null(response.Daylight);
        Assert.Null(response.Frost);
        Assert.Equal("summer", response.Season.Name);
        Assert.NotNull(response.Note); // defaults to northern hemisphere
    }

    [Fact]
    public void Build_DenverInJuly_ReturnsLosingDaylightWithMilestones()
    {
        var response = MakeService().Build(MidJuly2026, Denver);

        Assert.NotNull(response.Daylight);
        Assert.True(response.Daylight.TrendMinutesPerDay < 0);
        Assert.True(response.Daylight.DriftMinutes < 0);
        Assert.Equal("longest day", response.Daylight.DriftReference);
        // Only the single next milestone is returned; Denver sunsets drop below 8 pm in mid-August.
        var milestone = Assert.Single(response.Daylight.Milestones);
        Assert.Equal("Last 8pm sunset", milestone.Label);
        Assert.StartsWith("2026-08-1", milestone.Date);
    }

    [Fact]
    public void Build_SouthernHemisphere_WinterAndNoNote()
    {
        var response = MakeService().Build(MidJuly2026, new HomeLocation(-33.87, 151.21, "Australia/Sydney"));

        Assert.Equal("winter", response.Season.Name);
        Assert.Null(response.Note);
        Assert.NotNull(response.Daylight);
        Assert.Equal("shortest day", response.Daylight.DriftReference);
        Assert.True(response.Daylight.DriftMinutes > 0); // days growing since the June solstice
    }

    [Fact]
    public void Build_FrostFarAway_ShowsNoteInsteadOfFrost()
    {
        var service = MakeService(new()
        {
            ["FIRST_FROST"] = "10-07",
            ["LAST_FROST"] = "05-05",
        });

        // Mid-July: first frost is 80 days out — beyond the 6-week window.
        var response = service.Build(MidJuly2026, new HomeLocation(null, null, "UTC"));

        Assert.Null(response.Frost);
        Assert.NotNull(response.Note);
    }

    [Fact]
    public void Build_FrostWithinSixWeeks_TakesTheNoteSlot()
    {
        var service = MakeService(new()
        {
            ["FIRST_FROST"] = "10-07",
            ["LAST_FROST"] = "05-05",
        });

        var response = service.Build(new DateTimeOffset(2026, 9, 10, 18, 0, 0, TimeSpan.Zero), Denver);

        Assert.NotNull(response.Daylight); // slot 1
        Assert.NotNull(response.Frost); // slot 2 — bumps the note
        Assert.Null(response.Note);
        Assert.Equal("First frost", response.Frost.Label);
        Assert.Equal("2026-10-07", response.Frost.Date);
        Assert.Equal(27, response.Frost.DaysUntil);
    }

    [Fact]
    public void Build_NoDaylight_FrostAndNoteFillBothSlots()
    {
        var service = MakeService(new() { ["FIRST_FROST"] = "10-07" });

        var response = service.Build(
            new DateTimeOffset(2026, 9, 10, 18, 0, 0, TimeSpan.Zero), new HomeLocation(null, null, "UTC"));

        Assert.Null(response.Daylight);
        Assert.NotNull(response.Frost);
        Assert.NotNull(response.Note);
    }

    [Fact]
    public void Build_MalformedConfig_DegradesGracefully()
    {
        var service = MakeService(new() { ["FIRST_FROST"] = "13-45" });

        var response = service.Build(MidJuly2026, new HomeLocation(null, null, "Not/AZone"));

        Assert.Null(response.Daylight);
        Assert.Null(response.Frost);
        Assert.Equal("summer", response.Season.Name);
    }

    [Fact]
    public void Build_SameServiceNewLocation_UsesTheNewLocation()
    {
        var service = MakeService();

        var north = service.Build(MidJuly2026, Denver);
        var south = service.Build(MidJuly2026, new HomeLocation(-33.87, 151.21, "Australia/Sydney"));

        Assert.Equal("summer", north.Season.Name);
        Assert.Equal("winter", south.Season.Name);
    }
}
