namespace ServiceDefaults.Tests;

public sealed class HomeSettingsTests
{
    [Fact]
    public void Validate_WellFormed_ReturnsNull() =>
        Assert.Null(new HomeSettings(39.74, -104.99, "Denver", "America/Denver").Validate());

    [Fact]
    public void Validate_AllEmpty_ReturnsNull() =>
        Assert.Null(new HomeSettings(null, null, null, null).Validate());

    [Theory]
    [InlineData(39.0, null)]
    [InlineData(null, -104.0)]
    public void Validate_HalfACoordinatePair_Fails(double? lat, double? lon) =>
        Assert.NotNull(new HomeSettings(lat, lon, null, null).Validate());

    [Theory]
    [InlineData(-90.1, 0)]
    [InlineData(90.1, 0)]
    [InlineData(0, -180.1)]
    [InlineData(0, 180.1)]
    [InlineData(double.NaN, 0)]
    public void Validate_OutOfRange_Fails(double lat, double lon) =>
        Assert.NotNull(new HomeSettings(lat, lon, null, null).Validate());

    [Fact]
    public void Validate_LongName_Fails() =>
        Assert.NotNull(new HomeSettings(null, null, new string('x', HomeSettings.MaxNameLength + 1), null).Validate());

    [Theory]
    [InlineData("Not/AZone")]
    [InlineData("Central Standard Time")] // a Windows id — the renderer's browser can't use it
    public void Validate_UnknownZone_Fails(string zone) =>
        Assert.NotNull(new HomeSettings(null, null, null, zone).Validate());

    [Fact]
    public void Validate_BlankZone_TreatedAsUnset() =>
        Assert.Null(new HomeSettings(null, null, null, "").Validate());
}
