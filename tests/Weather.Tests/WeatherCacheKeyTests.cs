using Weather.Extensions;

namespace Weather.Tests;

public sealed class WeatherCacheKeyTests
{
    [Fact]
    public void CacheKey_SamePlace_IsStable() =>
        Assert.Equal("weather:39.740,-104.990", WebApplicationExtensions.CacheKey(39.74, -104.99));
}
