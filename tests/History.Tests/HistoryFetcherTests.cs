using System.Net;
using System.Text.Json;
using Hearth.TestUtilities;
using History.Records;
using Microsoft.Extensions.Logging.Abstractions;

namespace History.Tests;

public sealed class HistoryFetcherTests
{
    private const string Feed = """
        {"selected": [
            {"year": 2025, "text": "A recent thing happened.", "pages": [{"titles": {"normalized": "Recent thing"}}]},
            {"year": 1605, "text": " The Gunpowder Plot was foiled. ", "pages": [{"titles": {"normalized": "Gunpowder Plot"}}]},
            {"year": 1922, "text": "Tutankhamun's tomb was found.", "pages": []},
            {"text": "No year, skipped."},
            {"year": 1950, "text": ""}
        ]}
        """;

    private static HistoryFetcher MakeFetcher(FakeHttpMessageHandler handler) =>
        new(new HttpClient(handler), NullLogger<HistoryFetcher>.Instance);

    [Fact]
    public async Task FetchAsync_ValidFeed_ReturnsEventsOldestFirst()
    {
        var handler = new FakeHttpMessageHandler(Feed);

        var events = await MakeFetcher(handler).FetchAsync(new DateOnly(2026, 11, 4));

        Assert.EndsWith("/onthisday/selected/11/04", handler.LastRequestUri?.AbsolutePath);
        Assert.Equal([1605, 1922, 2025], events!.Select(e => e.Year));
        Assert.Equal("The Gunpowder Plot was foiled.", events[0].Text);
        Assert.Equal("Gunpowder Plot", events[0].PageTitle);
        Assert.Null(events[1].PageTitle);
    }

    [Theory]
    [InlineData("not-json", HttpStatusCode.OK)]
    [InlineData("", HttpStatusCode.ServiceUnavailable)]
    public async Task FetchAsync_MalformedOrError_ReturnsNull(string body, HttpStatusCode status)
    {
        Assert.Null(await MakeFetcher(new FakeHttpMessageHandler(body, status)).FetchAsync(new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public void Parse_NoSelected_ReturnsEmpty()
    {
        using var doc = JsonDocument.Parse("""{"events": []}""");

        Assert.Empty(HistoryFetcher.Parse(doc.RootElement));
    }

    [Fact]
    public void Spread_MoreThanCount_TakesOldestNewestAndEvenlyBetween()
    {
        var events = Enumerable.Range(0, 11).Select(i => new HistoryEvent(1900 + i * 10, $"e{i}", null)).ToList();

        var picked = HistoryFetcher.Spread(events, 3);

        Assert.Equal([1900, 1950, 2000], picked.Select(e => e.Year));
    }
}
