namespace Almanac.Tests;

public class SolarCalculatorTests
{
    private const double DenverLat = 39.74;
    private const double DenverLon = -104.99;

    private static readonly TimeZoneInfo Denver = TimeZoneInfo.FindSystemTimeZoneById("America/Denver");

    [Fact]
    public void SunTimesFor_DenverSummerSolstice_MatchesNoaaReference()
    {
        // NOAA solar calculator for Denver, 2026-06-21: sunrise 05:31, sunset 20:31 MDT.
        var times = SolarCalculator.SunTimesFor(new DateOnly(2026, 6, 21), DenverLat, DenverLon, Denver);

        Assert.NotNull(times);
        AssertWithinMinutes(new TimeSpan(5, 31, 0), times.Sunrise.TimeOfDay, 3);
        AssertWithinMinutes(new TimeSpan(20, 31, 0), times.Sunset.TimeOfDay, 3);
    }

    [Fact]
    public void DayLengthMinutes_DenverSolstices_SummerLongerThanWinter()
    {
        var summer = SolarCalculator.DayLengthMinutes(new DateOnly(2026, 6, 21), DenverLat);
        var winter = SolarCalculator.DayLengthMinutes(new DateOnly(2026, 12, 21), DenverLat);

        Assert.NotNull(summer);
        Assert.NotNull(winter);
        // Denver: ~14h59m at the June solstice, ~9h21m at the December solstice.
        Assert.InRange(summer.Value, 890, 910);
        Assert.InRange(winter.Value, 550, 570);
    }

    [Fact]
    public void DayLengthMinutes_Equator_RoughlyTwelveHoursYearRound()
    {
        foreach (var month in new[] { 3, 6, 9, 12 })
        {
            var length = SolarCalculator.DayLengthMinutes(new DateOnly(2026, month, 21), 0);
            Assert.NotNull(length);
            Assert.InRange(length.Value, 715, 740);
        }
    }

    [Theory]
    [InlineData(80, 12, 21)] // polar night
    [InlineData(80, 6, 21)] // polar day
    [InlineData(-80, 6, 21)] // polar night, southern hemisphere
    public void DayLengthMinutes_InsidePolarCircle_ReturnsNull(double latitude, int month, int day)
    {
        Assert.Null(SolarCalculator.DayLengthMinutes(new DateOnly(2026, month, day), latitude));
    }

    private static void AssertWithinMinutes(TimeSpan expected, TimeSpan actual, int toleranceMinutes)
    {
        var diff = Math.Abs((expected - actual).TotalMinutes);
        Assert.True(diff <= toleranceMinutes, $"Expected {expected} ± {toleranceMinutes}m but got {actual}.");
    }
}
