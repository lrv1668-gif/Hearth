using Weather.Extensions;

namespace Weather.Tests;

public sealed class WeatherCacheKeyTests
{
    [Fact]
    public void CacheKey_SamePlace_IsStable() =>
        Assert.Equal("weather:v2:imperial:39.740,-104.990", WebApplicationExtensions.CacheKey(39.74, -104.99, metric: false));

    [Fact]
    public void CacheKey_Units_KeepSeparateSnapshots() =>
        Assert.NotEqual(
            WebApplicationExtensions.CacheKey(39.74, -104.99, metric: false),
            WebApplicationExtensions.CacheKey(39.74, -104.99, metric: true));
}
