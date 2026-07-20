namespace Almanac.Tests;

public class SeasonCalculatorTests
{
    // 2026 June solstice: 2026-06-21 08:24 UTC. September equinox: 2026-09-23 00:05 UTC.
    private static readonly DateTimeOffset MidJuly2026 = new(2026, 7, 19, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void GetSeason_MidJulyNorthern_ReturnsSummerTowardAutumnEquinox()
    {
        var season = SeasonCalculator.GetSeason(MidJuly2026, isNorthern: true);

        Assert.Equal("summer", season.Name);
        Assert.Equal("Autumn equinox", season.NextMarkerName);
        Assert.Equal(94, season.TotalDays);
        Assert.Equal(29, season.DayOfSeason);
        Assert.InRange(season.Progress, 0.25, 0.35);
    }

    [Fact]
    public void GetSeason_MidJulySouthern_ReturnsWinterTowardSpringEquinox()
    {
        var season = SeasonCalculator.GetSeason(MidJuly2026, isNorthern: false);

        Assert.Equal("winter", season.Name);
        Assert.Equal("Spring equinox", season.NextMarkerName);
    }

    [Fact]
    public void GetSeason_JustAfterSolstice_StartsDayOne()
    {
        var justAfter = new DateTimeOffset(2026, 6, 21, 9, 0, 0, TimeSpan.Zero);
        var season = SeasonCalculator.GetSeason(justAfter, isNorthern: true);

        Assert.Equal("summer", season.Name);
        Assert.Equal(1, season.DayOfSeason);
        Assert.Equal("Early summer", season.Label);
    }

    [Fact]
    public void GetSeason_JustBeforeSolstice_IsLateSpring()
    {
        var justBefore = new DateTimeOffset(2026, 6, 21, 8, 0, 0, TimeSpan.Zero);
        var season = SeasonCalculator.GetSeason(justBefore, isNorthern: true);

        Assert.Equal("spring", season.Name);
        Assert.Equal("Late spring", season.Label);
        Assert.Equal("Summer solstice", season.NextMarkerName);
    }

    [Theory]
    [InlineData(6, 25, "Early summer")]
    [InlineData(8, 5, "Midsummer")]
    [InlineData(9, 15, "Late summer")]
    public void GetSeason_SummerThirds_LabelsEarlyMidLate(int month, int day, string expected)
    {
        var now = new DateTimeOffset(2026, month, day, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal(expected, SeasonCalculator.GetSeason(now, isNorthern: true).Label);
    }

    [Fact]
    public void GetSeason_MidAutumn_UsesHyphenatedLabel()
    {
        var now = new DateTimeOffset(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal("Mid-autumn", SeasonCalculator.GetSeason(now, isNorthern: true).Label);
    }

    [Fact]
    public void LastSolstice_MidJulyNorthern_IsJuneAndLongestDay()
    {
        var (instant, isLongestDay) = SeasonCalculator.LastSolstice(MidJuly2026, isNorthern: true);

        Assert.Equal(new DateTimeOffset(2026, 6, 21, 8, 24, 0, TimeSpan.Zero), instant);
        Assert.True(isLongestDay);
    }

    [Fact]
    public void LastSolstice_MidJulySouthern_IsShortestDay()
    {
        var (_, isLongestDay) = SeasonCalculator.LastSolstice(MidJuly2026, isNorthern: false);
        Assert.False(isLongestDay);
    }

    [Fact]
    public void GetSeason_OutsideMarkerTable_Throws()
    {
        var farFuture = new DateTimeOffset(2041, 6, 1, 0, 0, 0, TimeSpan.Zero);
        Assert.Throws<InvalidOperationException>(() => SeasonCalculator.GetSeason(farFuture, isNorthern: true));
    }
}
