using System.Net;
using Quote.Tests.Helpers;
using Xunit;

namespace Quote.Tests;

public sealed class QuoteFetcherTests
{
    private static QuoteFetcher MakeFetcher(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new FakeHttpMessageHandler(json, status);
        var http = new HttpClient(handler);
        return new QuoteFetcher(http);
    }

    [Fact]
    public async Task FetchAsync_ValidResponse_ReturnsMappedQuoteItem()
    {
        var fetcher = MakeFetcher("""[{"q":"Carpe diem","a":"Horace"}]""");
        var result = await fetcher.FetchAsync();
        Assert.NotNull(result);
        Assert.Equal("Carpe diem", result.Q);
        Assert.Equal("Horace", result.A);
    }

    [Fact]
    public async Task FetchAsync_EmptyArray_ReturnsNull()
    {
        var fetcher = MakeFetcher("[]");
        var result = await fetcher.FetchAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task FetchAsync_MultipleQuotes_ReturnsFirst()
    {
        var fetcher = MakeFetcher("""[{"q":"First","a":"Alice"},{"q":"Second","a":"Bob"}]""");
        var result = await fetcher.FetchAsync();
        Assert.NotNull(result);
        Assert.Equal("First", result.Q);
        Assert.Equal("Alice", result.A);
    }

    [Fact]
    public async Task FetchAsync_NetworkFailure_ReturnsNull()
    {
        var http = new HttpClient(new ThrowingHttpMessageHandler());
        var fetcher = new QuoteFetcher(http);

        var result = await fetcher.FetchAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task FetchAsync_HttpError_ReturnsNull()
    {
        var fetcher = MakeFetcher("", HttpStatusCode.ServiceUnavailable);
        var result = await fetcher.FetchAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task FetchAsync_MalformedJson_ReturnsNull()
    {
        var fetcher = MakeFetcher("not-json");
        var result = await fetcher.FetchAsync();
        Assert.Null(result);
    }
}
