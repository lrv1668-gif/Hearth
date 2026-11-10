using Microsoft.Extensions.Time.Testing;

namespace ServiceDefaults.Tests;

public sealed class OAuthStateStoreTests
{
    private readonly FakeTimeProvider _time = new(DateTimeOffset.Parse("2026-11-09T12:00:00Z"));
    private readonly OAuthStateStore _states;

    public OAuthStateStoreTests() => _states = new OAuthStateStore(_time);

    [Fact]
    public void Issue_ReturnsDistinctStatesThatConsumeOnce()
    {
        var first = _states.Issue();
        var second = _states.Issue();

        Assert.NotEqual(first, second);
        Assert.True(_states.TryConsume(first));
        Assert.False(_states.TryConsume(first));
        Assert.True(_states.TryConsume(second));
    }

    [Fact]
    public void TryConsume_UnknownState_ReturnsFalse() =>
        Assert.False(_states.TryConsume("never-issued"));

    [Fact]
    public void TryConsume_AtLifetimeBoundary_ReturnsTrue()
    {
        _states.Remember("s");
        _time.Advance(OAuthStateStore.Lifetime);

        Assert.True(_states.TryConsume("s"));
    }

    [Fact]
    public void TryConsume_PastLifetime_ReturnsFalseAndForgetsIt()
    {
        _states.Remember("s");
        _time.Advance(OAuthStateStore.Lifetime + TimeSpan.FromTicks(1));

        Assert.False(_states.TryConsume("s"));
        Assert.Equal(0, _states.Count);
    }

    [Fact]
    public void Remember_SameStateAgain_RestartsItsLifetime()
    {
        _states.Remember("s");
        _time.Advance(TimeSpan.FromMinutes(6));
        _states.Remember("s");
        _time.Advance(TimeSpan.FromMinutes(6));

        Assert.True(_states.TryConsume("s"));
    }

    [Fact]
    public void Remember_SweepsExpiredStates()
    {
        for (var i = 0; i < 100; i++) _states.Issue();
        _time.Advance(OAuthStateStore.Lifetime + TimeSpan.FromSeconds(1));

        var fresh = _states.Issue();

        Assert.Equal(1, _states.Count);
        Assert.True(_states.TryConsume(fresh));
    }
}
