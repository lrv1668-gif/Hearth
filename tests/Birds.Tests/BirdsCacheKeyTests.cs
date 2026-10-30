using Birds.Extensions;

namespace Birds.Tests;

public sealed class BirdsCacheKeyTests
{
    [Fact]
    public void CacheKey_NewLocation_GetsItsOwnEntry() =>
        Assert.NotEqual(WebApplicationExtensions.CacheKey(39.74, -104.99, 15), WebApplicationExtensions.CacheKey(51.5, -0.12, 15));

    [Fact]
    public void CacheKey_NewRadius_GetsItsOwnEntry() =>
        Assert.NotEqual(WebApplicationExtensions.CacheKey(39.74, -104.99, 15), WebApplicationExtensions.CacheKey(39.74, -104.99, 30));
}
