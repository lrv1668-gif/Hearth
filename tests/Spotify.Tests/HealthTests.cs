using Xunit;

namespace Spotify.Tests;

public sealed class HealthTests
{
    [Fact]
    public void Evaluate_AllVarsPresent_ReturnsConfiguredWithEmptyMissing()
    {
        var health = Health.Evaluate(
            ("SPOTIFY_CLIENT_ID", "id"),
            ("SPOTIFY_CLIENT_SECRET", "secret"),
            ("SPOTIFY_REDIRECT_URI", "http://localhost/spotify/callback"));

        Assert.True(health.Configured);
        Assert.Empty(health.Missing);
    }

    [Fact]
    public void Evaluate_OneVarMissing_ReturnsUnconfiguredWithThatName()
    {
        var health = Health.Evaluate(
            ("SPOTIFY_CLIENT_ID", "id"),
            ("SPOTIFY_CLIENT_SECRET", null),
            ("SPOTIFY_REDIRECT_URI", "http://localhost/spotify/callback"));

        Assert.False(health.Configured);
        Assert.Equal(["SPOTIFY_CLIENT_SECRET"], health.Missing);
    }

    [Fact]
    public void Evaluate_EmptyStringValue_TreatedAsMissing()
    {
        var health = Health.Evaluate(
            ("SPOTIFY_CLIENT_ID", ""),
            ("SPOTIFY_CLIENT_SECRET", "secret"),
            ("SPOTIFY_REDIRECT_URI", "http://localhost/spotify/callback"));

        Assert.False(health.Configured);
        Assert.Equal(["SPOTIFY_CLIENT_ID"], health.Missing);
    }

    [Fact]
    public void Evaluate_AllVarsMissing_ListsAllNamesInOrder()
    {
        var health = Health.Evaluate(
            ("SPOTIFY_CLIENT_ID", null),
            ("SPOTIFY_CLIENT_SECRET", null),
            ("SPOTIFY_REDIRECT_URI", null));

        Assert.False(health.Configured);
        Assert.Equal(["SPOTIFY_CLIENT_ID", "SPOTIFY_CLIENT_SECRET", "SPOTIFY_REDIRECT_URI"], health.Missing);
    }
}
