using Xunit;

namespace Calendar.Tests;

public sealed class HealthTests
{
    [Fact]
    public void Evaluate_AllVarsPresent_ReturnsConfiguredWithEmptyMissing()
    {
        var health = Health.Evaluate(
            ("GOOGLE_CLIENT_ID", "id"),
            ("GOOGLE_CLIENT_SECRET", "secret"),
            ("GOOGLE_REDIRECT_URI", "http://localhost/calendar/google/callback"));

        Assert.True(health.Configured);
        Assert.Empty(health.Missing);
    }

    [Fact]
    public void Evaluate_OneVarMissing_ReturnsUnconfiguredWithThatName()
    {
        var health = Health.Evaluate(
            ("GOOGLE_CLIENT_ID", "id"),
            ("GOOGLE_CLIENT_SECRET", null),
            ("GOOGLE_REDIRECT_URI", "http://localhost/calendar/google/callback"));

        Assert.False(health.Configured);
        Assert.Equal(["GOOGLE_CLIENT_SECRET"], health.Missing);
    }

    [Fact]
    public void Evaluate_EmptyStringValue_TreatedAsMissing()
    {
        var health = Health.Evaluate(
            ("GOOGLE_CLIENT_ID", ""),
            ("GOOGLE_CLIENT_SECRET", "secret"),
            ("GOOGLE_REDIRECT_URI", "http://localhost/calendar/google/callback"));

        Assert.False(health.Configured);
        Assert.Equal(["GOOGLE_CLIENT_ID"], health.Missing);
    }

    [Fact]
    public void Evaluate_AllVarsMissing_ListsAllNamesInOrder()
    {
        var health = Health.Evaluate(
            ("GOOGLE_CLIENT_ID", null),
            ("GOOGLE_CLIENT_SECRET", null),
            ("GOOGLE_REDIRECT_URI", null));

        Assert.False(health.Configured);
        Assert.Equal(["GOOGLE_CLIENT_ID", "GOOGLE_CLIENT_SECRET", "GOOGLE_REDIRECT_URI"], health.Missing);
    }
}
