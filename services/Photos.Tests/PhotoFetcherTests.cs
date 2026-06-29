using System.Net;
using Photos.Tests.Helpers;
using Xunit;

namespace Photos.Tests;

public sealed class PhotoFetcherTests
{
    private const string ValidJson = """
    [
      {
        "id": "abc",
        "urls": { "regular": "https://img/abc" },
        "user": { "name": "Jane" },
        "links": { "html": "https://unsplash/abc" },
        "description": "A tree",
        "alt_description": "alt tree"
      },
      {
        "id": "def",
        "urls": { "regular": "https://img/def" },
        "user": { "name": "Bob" },
        "links": { "html": "https://unsplash/def" },
        "description": null,
        "alt_description": "fallback alt"
      }
    ]
    """;

    private static PhotoFetcher MakeFetcher(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new FakeHttpMessageHandler(json, status);
        var http = new HttpClient(handler);
        return new PhotoFetcher(http);
    }

    [Fact]
    public async Task FetchAsync_ValidArray_MapsPhotoFields()
    {
        var fetcher = MakeFetcher(ValidJson);

        var photos = await fetcher.FetchAsync("nature", "landscape", "fake-key");

        Assert.Equal(2, photos.Count);
        var first = photos[0];
        Assert.Equal("abc", first.Id);
        Assert.Equal("https://img/abc", first.Url);
        Assert.Equal("Jane", first.PhotographerName);
        Assert.Equal("https://unsplash/abc", first.UnsplashLink);
        Assert.Equal("A tree", first.Description);
        Assert.Equal("unsplash", first.Source);
    }

    [Fact]
    public async Task FetchAsync_DescriptionNull_FallsBackToAltDescription()
    {
        var fetcher = MakeFetcher(ValidJson);

        var photos = await fetcher.FetchAsync("nature", "landscape", "fake-key");

        Assert.Equal("fallback alt", photos[1].Description);
    }

    [Fact]
    public async Task FetchAsync_EmptyArray_ReturnsEmptyList()
    {
        var fetcher = MakeFetcher("[]");

        var photos = await fetcher.FetchAsync("nature", "landscape", "fake-key");

        Assert.Empty(photos);
    }

    [Fact]
    public async Task FetchAsync_HttpError_ThrowsHttpRequestException()
    {
        var fetcher = MakeFetcher("", HttpStatusCode.Unauthorized);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => fetcher.FetchAsync("nature", "landscape", "bad-key"));
    }
}
