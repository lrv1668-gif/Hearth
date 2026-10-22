using Weather.Extensions;
using Xunit;

namespace Weather.Tests;

public sealed class WeatherCacheKeyTests
{
    [Fact]
    public void CacheKey_NewLocation_GetsItsOwnEntry() =>
        Assert.NotEqual(WebApplicationExtensions.CacheKey(39.74, -104.99), WebApplicationExtensions.CacheKey(51.5, -0.12));

    [Fact]
    public void CacheKey_SamePlace_IsStable() =>
        Assert.Equal("weather:39.740,-104.990", WebApplicationExtensions.CacheKey(39.74, -104.99));
}
