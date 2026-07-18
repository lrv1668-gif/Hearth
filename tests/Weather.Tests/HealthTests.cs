using Xunit;

namespace Weather.Tests;

public sealed class HealthTests
{
    [Fact]
    public void Evaluate_AllVarsPresent_ReturnsConfiguredWithEmptyMissing()
    {
        var health = Health.Evaluate(("LATITUDE", "40.0"), ("LONGITUDE", "-75.0"));

        Assert.True(health.Configured);
        Assert.Empty(health.Missing);
    }

    [Fact]
    public void Evaluate_OneVarMissing_ReturnsUnconfiguredWithThatName()
    {
        var health = Health.Evaluate(("LATITUDE", "40.0"), ("LONGITUDE", null));

        Assert.False(health.Configured);
        Assert.Equal(["LONGITUDE"], health.Missing);
    }

    [Fact]
    public void Evaluate_EmptyStringValue_TreatedAsMissing()
    {
        var health = Health.Evaluate(("LATITUDE", ""), ("LONGITUDE", "-75.0"));

        Assert.False(health.Configured);
        Assert.Equal(["LATITUDE"], health.Missing);
    }

    [Fact]
    public void Evaluate_AllVarsMissing_ListsAllNamesInOrder()
    {
        var health = Health.Evaluate(("LATITUDE", null), ("LONGITUDE", null));

        Assert.False(health.Configured);
        Assert.Equal(["LATITUDE", "LONGITUDE"], health.Missing);
    }
}
