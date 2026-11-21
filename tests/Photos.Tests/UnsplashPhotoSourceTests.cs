using System.Net;
using Hearth.TestUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ServiceDefaults;
using ZiggyCreatures.Caching.Fusion;

namespace Photos.Tests;

/// <summary><see cref="UnsplashPhotoSource.GetRandomAsync"/> — the seasonal query and its cache.</summary>
public sealed class UnsplashPhotoSourceTests
{
    private const string Batch = """
    [{ "id": "a", "urls": { "regular": "https://img/a" }, "user": { "name": "Jane" }, "links": { "html": "https://unsplash/a" } }]
    """;

    private readonly FakeHttpMessageHandler _unsplash = new(Batch);
    private readonly FusionCache _cache = new(new FusionCacheOptions());

    private UnsplashPhotoSource Source(string? latitude = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UNSPLASH_ACCESS_KEY"] = "key",
                ["LATITUDE"] = latitude,
                ["LONGITUDE"] = latitude is null ? null : "151.2",
            })
            .Build();
        // No DISPLAY_URL: the home location comes from the env-var fallbacks
        var home = new HomeLocationClient(new HttpClient(), _cache, config, NullLogger<HomeLocationClient>.Instance);
        return new UnsplashPhotoSource(
            _cache, new PhotoFetcher(new HttpClient(_unsplash)), home, config, NullLogger<UnsplashPhotoSource>.Instance);
    }

    private string RequestedQuery() =>
        Uri.UnescapeDataString(_unsplash.LastRequestUri!.Query.Split('&')[0]["?query=".Length..]);

    private static string Expected(bool isNorthern) =>
        SeasonalQuery.Expand("seasonal", DateOnly.FromDateTime(DateTime.UtcNow), isNorthern);

    [Fact]
    public async Task GetRandomAsync_SouthernHomeLocation_FlipsTheSeason()
    {
        await Source(latitude: "-33.9").GetRandomAsync(new PhotoSourceContext("landscape", "seasonal"));

        Assert.Equal(Expected(isNorthern: false), RequestedQuery());
    }

    [Fact]
    public async Task GetRandomAsync_NoHomeLocation_UsesNorthernSeasons()
    {
        await Source().GetRandomAsync(new PhotoSourceContext("landscape", "seasonal"));

        Assert.Equal(Expected(isNorthern: true), RequestedQuery());
    }

    [Fact]
    public async Task GetRandomAsync_SecondCall_ServesTheCachedBatch()
    {
        var source = Source();

        await source.GetRandomAsync(new PhotoSourceContext("landscape", "nature"));
        var photo = await source.GetRandomAsync(new PhotoSourceContext("landscape", "nature"));

        Assert.Equal("a", photo?.Id);
        Assert.Equal(1, _unsplash.RequestCount);
    }

    [Fact]
    public async Task GetRandomAsync_EmptyBatch_IsNotCached()
    {
        _unsplash.ResponseJson = "[]";
        var source = Source();

        Assert.Null(await source.GetRandomAsync(new PhotoSourceContext("landscape", "nature")));
        _unsplash.ResponseJson = Batch;
        var photo = await source.GetRandomAsync(new PhotoSourceContext("landscape", "nature"));

        Assert.Equal("a", photo?.Id);
        Assert.Equal(2, _unsplash.RequestCount);
    }

    [Fact]
    public async Task GetRandomAsync_UnsplashDownWithNothingCached_ReturnsNull()
    {
        _unsplash.Status = HttpStatusCode.ServiceUnavailable;

        Assert.Null(await Source().GetRandomAsync(new PhotoSourceContext("landscape", "nature")));
    }
}
