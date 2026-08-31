using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Almanac.Tests;

public class AlmanacServiceTests
{
    private static readonly DateTimeOffset MidJuly2026 = new(2026, 7, 19, 18, 0, 0, TimeSpan.Zero);

    private static AlmanacService MakeService(Dictionary<string, string?> values)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return new AlmanacService(config, NullLogger<AlmanacService>.Instance);
    }

    [Fact]
    public void Build_NoCoordinates_OmitsDaylightButKeepsSeasonAndNote()
    {
        var service = MakeService(new() { ["TZ"] = "UTC" });

        var response = service.Build(MidJuly2026);

        Assert.Null(response.Daylight);
        Assert.Null(response.Frost);
        Assert.Equal("summer", response.Season.Name);
        Assert.NotNull(response.Note); // defaults to northern hemisphere
    }

    [Fact]
    public void Build_DenverInJuly_ReturnsLosingDaylightWithMilestones()
    {
        var service = MakeService(new()
        {
            ["LATITUDE"] = "39.74",
            ["LONGITUDE"] = "-104.99",
            ["TZ"] = "America/Denver",
        });

        var response = service.Build(MidJuly2026);

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
        var service = MakeService(new()
        {
            ["LATITUDE"] = "-33.87",
            ["LONGITUDE"] = "151.21",
            ["TZ"] = "Australia/Sydney",
        });

        var response = service.Build(MidJuly2026);

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
            ["TZ"] = "UTC",
            ["FIRST_FROST"] = "10-07",
            ["LAST_FROST"] = "05-05",
        });

        // Mid-July: first frost is 80 days out — beyond the 6-week window.
        var response = service.Build(MidJuly2026);

        Assert.Null(response.Frost);
        Assert.NotNull(response.Note);
    }

    [Fact]
    public void Build_FrostWithinSixWeeks_TakesTheNoteSlot()
    {
        var service = MakeService(new()
        {
            ["LATITUDE"] = "39.74",
            ["LONGITUDE"] = "-104.99",
            ["TZ"] = "America/Denver",
            ["FIRST_FROST"] = "10-07",
            ["LAST_FROST"] = "05-05",
        });

        var response = service.Build(new DateTimeOffset(2026, 9, 10, 18, 0, 0, TimeSpan.Zero));

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
        var service = MakeService(new()
        {
            ["TZ"] = "UTC",
            ["FIRST_FROST"] = "10-07",
        });

        var response = service.Build(new DateTimeOffset(2026, 9, 10, 18, 0, 0, TimeSpan.Zero));

        Assert.Null(response.Daylight);
        Assert.NotNull(response.Frost);
        Assert.NotNull(response.Note);
    }

    [Fact]
    public void Build_MalformedConfig_DegradesGracefully()
    {
        var service = MakeService(new()
        {
            ["TZ"] = "UTC",
            ["LATITUDE"] = "not-a-number",
            ["LONGITUDE"] = "-104.99",
            ["FIRST_FROST"] = "13-45",
        });

        var response = service.Build(MidJuly2026);

        Assert.Null(response.Daylight);
        Assert.Null(response.Frost);
        Assert.Equal("summer", response.Season.Name);
    }
}
