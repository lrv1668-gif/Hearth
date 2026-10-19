using Data;
using Hearth.TestUtilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
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

    [Fact]
    public async Task GetOrSetAsync_FreshEntry_DoesNotCallFactoryAgain()
    {
        using var sp = BuildProvider();
        var cache = sp.GetRequiredService<IFusionCache>();
        var calls = 0;

        await cache.GetOrSetAsync<string>("k", _ => { calls++; return Task.FromResult("v"); }, Options);
        await cache.GetOrSetAsync<string>("k", _ => { calls++; return Task.FromResult("v"); }, Options);

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task GetOrSetAsync_StaleEntryAndFactoryThrows_ServesStaleValue()
    {
        using var sp = BuildProvider();
        var cache = sp.GetRequiredService<IFusionCache>();
        await cache.SetAsync("k", "last good", Options);
        await cache.ExpireAsync("k");

        var value = await cache.GetOrSetAsync<string>(
            "k", _ => throw new HttpRequestException("upstream down"), Options);

        Assert.Equal("last good", value);
    }

    [Fact]
    public async Task GetOrSetAsync_NothingCachedAndFactoryThrows_Throws()
    {
        using var sp = BuildProvider();
        var cache = sp.GetRequiredService<IFusionCache>();

        await Assert.ThrowsAsync<HttpRequestException>(() => cache.GetOrSetAsync<string>(
            "k", _ => throw new HttpRequestException("upstream down"), Options).AsTask());
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

    private sealed record Payload(string Name, int Count);
}
