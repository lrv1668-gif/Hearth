using Data;
using Hearth.TestUtilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using ZiggyCreatures.Caching.Fusion;

namespace ServiceDefaults.Tests;

public sealed class HearthCacheTests
{
    private static ServiceProvider BuildProvider(IDistributedCache? l2 = null)
    {
        var services = new ServiceCollection();
        if (l2 is not null) services.AddSingleton(l2);
        services.AddHearthCache();
        return services.BuildServiceProvider();
    }

    private static readonly FusionCacheEntryOptions Options = new()
    {
        Duration = TimeSpan.FromMinutes(30),
        IsFailSafeEnabled = true,
        FailSafeMaxDuration = TimeSpan.FromHours(1),
    };

    // No entry options here: these check the defaults AddHearthCache sets, not FusionCache's
    [Fact]
    public async Task GetOrSetAsync_StaleEntryAndFactoryThrows_ServesStaleValueByDefault()
    {
        using var sp = BuildProvider();
        var cache = sp.GetRequiredService<IFusionCache>();
        await cache.SetAsync("k", "last good");
        await cache.ExpireAsync("k");

        var value = await cache.GetOrSetAsync<string>("k", _ => throw new HttpRequestException("upstream down"));

        Assert.Equal("last good", value);
    }

    [Fact]
    public async Task GetOrSetAsync_NothingCachedAndFactoryThrows_Throws()
    {
        using var sp = BuildProvider();
        var cache = sp.GetRequiredService<IFusionCache>();

        await Assert.ThrowsAsync<HttpRequestException>(() => cache.GetOrSetAsync<string>(
            "k", _ => throw new HttpRequestException("upstream down")).AsTask());
    }

    [Fact]
    public async Task GetOrSetAsync_SqliteL2_SurvivesRestart()
    {
        using var tmp = new TempDatabase();

        using (var first = BuildProvider(new SqliteDistributedCache(tmp.Db)))
        {
            await first.GetRequiredService<IFusionCache>().SetAsync("k", new Payload("cached", 3), Options);
        }

        using var second = BuildProvider(new SqliteDistributedCache(tmp.Db));
        var value = await second.GetRequiredService<IFusionCache>().GetOrSetAsync<Payload>(
            "k", _ => throw new InvalidOperationException("factory should not run"), Options);

        Assert.Equal(new Payload("cached", 3), value);
    }

    [Fact]
    public async Task GetOrSetAsync_ValueTupleThroughL2_RoundTrips()
    {
        using var tmp = new TempDatabase();

        using (var first = BuildProvider(new SqliteDistributedCache(tmp.Db)))
        {
            await first.GetRequiredService<IFusionCache>().SetAsync("k", (Name: "cached", Count: 3), Options);
        }

        using var second = BuildProvider(new SqliteDistributedCache(tmp.Db));
        var value = await second.GetRequiredService<IFusionCache>().GetOrSetAsync<(string Name, int Count)>(
            "k", _ => throw new InvalidOperationException("factory should not run"), Options);

        Assert.Equal(("cached", 3), value);
    }

    [Fact]
    public async Task GetOrSetAsync_StaleEntryAndSlowFactory_ServesStaleThenRefreshesInBackground()
    {
        using var sp = BuildProvider();
        var cache = sp.GetRequiredService<IFusionCache>();
        var options = Options.Duplicate();
        options.FactorySoftTimeout = HearthCache.SoftTimeout;
        await cache.SetAsync("k", "last good", options);
        await cache.ExpireAsync("k");
        var release = new TaskCompletionSource();

        var value = await cache.GetOrSetAsync<string>("k", async _ =>
        {
            await release.Task;
            return "fresh";
        }, options);

        Assert.Equal("last good", value);

        release.SetResult();
        string? refreshed = null;
        for (var i = 0; i < 100 && refreshed != "fresh"; i++)
        {
            await Task.Delay(50);
            refreshed = await cache.GetOrDefaultAsync<string>("k");
        }
        Assert.Equal("fresh", refreshed);
    }

    private sealed record Payload(string Name, int Count);
}
