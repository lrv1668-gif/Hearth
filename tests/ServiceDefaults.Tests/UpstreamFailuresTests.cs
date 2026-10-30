using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Time.Testing;
using ZiggyCreatures.Caching.Fusion;

namespace ServiceDefaults.Tests;

public sealed class UpstreamFailuresTests : IAsyncDisposable
{
    private readonly FakeTimeProvider _time = new(DateTimeOffset.Parse("2026-10-24T12:00:00Z"));
    private readonly ServiceProvider _provider;
    private readonly IFusionCache _cache;
    private readonly UpstreamFailures _failures;

    public UpstreamFailuresTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(_time);
        services.AddHearthCache();
        _provider = services.BuildServiceProvider();
        _cache = _provider.GetRequiredService<IFusionCache>();
        _failures = _provider.GetRequiredService<UpstreamFailures>();
    }

    public ValueTask DisposeAsync() => _provider.DisposeAsync();

    // FusionCache raises its events on background threads, so wait for them to land.
    private static async Task Until(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition() && DateTime.UtcNow < deadline) await Task.Delay(10);
        Assert.True(condition());
    }

    private async Task FailAsync(string key)
    {
        await Assert.ThrowsAnyAsync<Exception>(() => _cache.GetOrSetAsync<string>(
            key, (_, _) => throw new HttpRequestException("down")).AsTask());
        await Until(() => _failures.Failing().Count > 0);
    }

    private Task<HealthCheckResult> CheckAsync() => _failures.CheckHealthAsync(new HealthCheckContext());

    [Fact]
    public async Task Check_NothingFailed_Healthy()
    {
        Assert.Equal(HealthStatus.Healthy, (await CheckAsync()).Status);
    }

    [Fact]
    public async Task Check_FetchFailed_DegradedNamingTheSourceOnly()
    {
        await FailAsync("weather:39.740,-104.990");

        var result = await CheckAsync();

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("weather", result.Description);
        Assert.DoesNotContain("39.740", result.Description);
    }

    [Fact]
    public async Task Check_FailedThenSucceeded_Healthy()
    {
        await FailAsync("quote:today");
        await _cache.GetOrSetAsync<string>("quote:today", (_, _) => Task.FromResult("ok"));

        await Until(() => _failures.Failing().Count == 0);
        Assert.Equal(HealthStatus.Healthy, (await CheckAsync()).Status);
    }

    [Fact]
    public async Task Check_FailureOlderThanWindow_Healthy()
    {
        await FailAsync("rss:https://example.com/feed");

        _time.Advance(UpstreamFailures.Window + TimeSpan.FromMinutes(1));

        Assert.Equal(HealthStatus.Healthy, (await CheckAsync()).Status);
    }

    [Fact]
    public void AddHearthCache_CalledTwice_RegistersOneCheck()
    {
        var services = new ServiceCollection();
        services.AddHearthCache();
        services.AddHearthCache();

        Assert.Single(services, d => d.ServiceType == typeof(UpstreamFailures));
    }
}
