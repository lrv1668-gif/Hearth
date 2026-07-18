using Xunit;

namespace Birds.Tests;

public sealed class HealthTests
{
    [Fact]
    public void Evaluate_AllVarsPresent_ReturnsConfiguredWithEmptyMissing()
    {
        var health = Health.Evaluate(
            ("EBIRD_API_KEY", "abc123"),
            ("LATITUDE", "40.0"),
            ("LONGITUDE", "-75.0"));

        Assert.True(health.Configured);
        Assert.Empty(health.Missing);
    }

    [Fact]
    public void Evaluate_OneVarMissing_ReturnsUnconfiguredWithThatName()
    {
        var health = Health.Evaluate(
            ("EBIRD_API_KEY", null),
            ("LATITUDE", "40.0"),
            ("LONGITUDE", "-75.0"));

        Assert.False(health.Configured);
        Assert.Equal(["EBIRD_API_KEY"], health.Missing);
    }

    [Fact]
    public void Evaluate_EmptyStringValue_TreatedAsMissing()
    {
        var health = Health.Evaluate(
            ("EBIRD_API_KEY", ""),
            ("LATITUDE", "40.0"),
            ("LONGITUDE", "-75.0"));

        Assert.False(health.Configured);
        Assert.Equal(["EBIRD_API_KEY"], health.Missing);
    }

    [Fact]
    public void Evaluate_AllVarsMissing_ListsAllNamesInOrder()
    {
        var health = Health.Evaluate(
            ("EBIRD_API_KEY", null),
            ("LATITUDE", null),
            ("LONGITUDE", null));

        Assert.False(health.Configured);
        Assert.Equal(["EBIRD_API_KEY", "LATITUDE", "LONGITUDE"], health.Missing);
    }
}
