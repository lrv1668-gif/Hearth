using System.Net;
using Hearth.TestUtilities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace History.Tests;

public sealed class HistoryEndpointTests : IDisposable
{
    private readonly FakeHttpMessageHandler _wikipedia = new("""
        {"selected": [{"year": 1922, "text": "Tutankhamun's tomb was found.", "pages": [{"titles": {"normalized": "KV62"}}]}]}
        """);
    private readonly WebApplicationFactory<Program> _factory;

    public HistoryEndpointTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
            host.ConfigureTestServices(services =>
            {
                services.AddHttpClient<HistoryFetcher>().ConfigurePrimaryHttpMessageHandler(() => _wikipedia);
                services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero)));
            }));
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task OnThisDay_ReturnsTodaysEventsSnakeCased()
    {
        var json = await _factory.CreateClient().GetStringAsync("/history/onthisday");

        Assert.Contains("\"date\":\"2026-11-04\"", json);
        Assert.Contains("\"page_title\":\"KV62\"", json);
        Assert.Contains("\"year\":1922", json);
    }

    [Fact]
    public async Task OnThisDay_CalledTwice_FetchesOnce()
    {
        var client = _factory.CreateClient();

        await client.GetStringAsync("/history/onthisday");
        await client.GetStringAsync("/history/onthisday");

        Assert.Equal(1, _wikipedia.RequestCount);
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _factory.CreateClient().GetAsync("/history/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
