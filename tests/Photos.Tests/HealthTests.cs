using Xunit;

namespace Photos.Tests;

public sealed class HealthTests
{
    [Fact]
    public void Evaluate_AllVarsPresent_ReturnsConfiguredWithEmptyMissing()
    {
        var health = Health.Evaluate(("UNSPLASH_ACCESS_KEY", "abc123"));

        Assert.True(health.Configured);
        Assert.Empty(health.Missing);
    }

    [Fact]
    public void Evaluate_OneVarMissing_ReturnsUnconfiguredWithThatName()
    {
        var health = Health.Evaluate(("UNSPLASH_ACCESS_KEY", null));

        Assert.False(health.Configured);
        Assert.Equal(["UNSPLASH_ACCESS_KEY"], health.Missing);
    }

    [Fact]
    public void Evaluate_EmptyStringValue_TreatedAsMissing()
    {
        var health = Health.Evaluate(("UNSPLASH_ACCESS_KEY", ""));

        Assert.False(health.Configured);
        Assert.Equal(["UNSPLASH_ACCESS_KEY"], health.Missing);
    }
}
