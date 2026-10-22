using System.Net;
using Hearth.TestUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

namespace ServiceDefaults.Tests;

public sealed class HomeLocationClientTests
{
    private static readonly Dictionary<string, string?> Env = new()
    {
        ["LATITUDE"] = "39.74",
        ["LONGITUDE"] = "-104.99",
        ["TZ"] = "America/Denver",
    };

    private sealed class DownHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            throw new HttpRequestException("connection refused");
        }
    }

    private sealed class CountingHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(status));
        }
    }

    private static HomeLocationClient Client(HttpMessageHandler handler, Dictionary<string, string?> env, bool displayUrl = true)
    {
        var services = new ServiceCollection();
        services.AddHearthCache();
        var cache = services.BuildServiceProvider().GetRequiredService<IFusionCache>();
        var http = new HttpClient(handler);
        if (displayUrl) http.BaseAddress = new Uri("http://display:8090/");
        return new HomeLocationClient(
            http, cache, new ConfigurationBuilder().AddInMemoryCollection(env).Build(),
            NullLogger<HomeLocationClient>.Instance);
    }

    [Fact]
    public async Task GetAsync_Stored_WinsOverEnv()
    {
        var handler = new FakeHttpMessageHandler(
            """{"latitude":51.5,"longitude":-0.12,"name":"London","time_zone":"Europe/London"}""");

        var home = await Client(handler, Env).GetAsync();

        Assert.Equal(new HomeLocation(51.5, -0.12, "Europe/London"), home);
        Assert.Equal("http://display:8090/display/home", handler.LastRequestUri?.AbsoluteUri);
    }

    [Fact]
    public async Task GetAsync_NothingStored_UsesEnv()
    {
        var home = await Client(new FakeHttpMessageHandler("", HttpStatusCode.NoContent), Env).GetAsync();

        Assert.Equal(new HomeLocation(39.74, -104.99, "America/Denver"), home);
    }

    [Fact]
    public async Task GetAsync_StoredZoneOnly_KeepsEnvCoordinates()
    {
        var handler = new FakeHttpMessageHandler("""{"latitude":null,"longitude":null,"time_zone":"Europe/London"}""");

        var home = await Client(handler, Env).GetAsync();

        Assert.Equal(new HomeLocation(39.74, -104.99, "Europe/London"), home);
    }

    [Fact]
    public async Task GetAsync_DisplayDown_UsesEnv()
    {
        var home = await Client(new DownHandler(), Env).GetAsync();

        Assert.Equal(new HomeLocation(39.74, -104.99, "America/Denver"), home);
    }

    [Fact]
    public async Task GetAsync_DisplayDownTwice_TriesOnceThenRemembers()
    {
        var handler = new DownHandler();
        var client = Client(handler, Env);

        await client.GetAsync();
        await client.GetAsync();

        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task GetAsync_NothingStoredTwice_CachesTheEmptyAnswer()
    {
        var handler = new CountingHandler(HttpStatusCode.NoContent);
        var client = Client(handler, Env);

        await client.GetAsync();
        var home = await client.GetAsync();

        Assert.Equal(1, handler.Calls);
        Assert.Equal(new HomeLocation(39.74, -104.99, "America/Denver"), home);
    }

    [Fact]
    public async Task GetAsync_NoDisplayUrl_UsesEnvWithoutCalling()
    {
        var handler = new FakeHttpMessageHandler("{}");

        var home = await Client(handler, Env, displayUrl: false).GetAsync();

        Assert.Equal(new HomeLocation(39.74, -104.99, "America/Denver"), home);
        Assert.Null(handler.LastRequestUri);
    }

    [Fact]
    public async Task GetAsync_NothingAnywhere_HasNoCoordinates()
    {
        var home = await Client(new FakeHttpMessageHandler("", HttpStatusCode.NoContent), []).GetAsync();

        Assert.Null(home.Coordinates);
        Assert.Null(home.TimeZone);
    }

    [Fact]
    public async Task GetAsync_MalformedEnv_HasNoCoordinates()
    {
        var env = new Dictionary<string, string?> { ["LATITUDE"] = "not-a-number", ["LONGITUDE"] = "-104.99" };

        var home = await Client(new DownHandler(), env).GetAsync();

        Assert.Null(home.Coordinates);
    }
}
