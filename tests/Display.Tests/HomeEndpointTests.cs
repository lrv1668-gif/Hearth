using System.Net;
using System.Net.Http.Json;
using Data.Abstractions;
using Hearth.TestUtilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ServiceDefaults;
using Xunit;

namespace Display.Tests;

/// <summary>/display/home and the time zone /display/preview hands the renderer.</summary>
public sealed class HomeEndpointTests : IDisposable
{
    private readonly TempDatabase _db = new();
    private readonly FakeHttpMessageHandler _renderer = new("png");
    private readonly WebApplicationFactory<Program> _factory;

    public HomeEndpointTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
        {
            host.UseSetting("RENDERER_URL", "http://renderer:3000");
            host.ConfigureTestServices(services =>
            {
                services.AddKeyedSingleton<IDatabase>("display", _db.Db);
                services.AddHttpClient<RendererClient>().ConfigurePrimaryHttpMessageHandler(() => _renderer);
            });
        });
    }

    public void Dispose()
    {
        _factory.Dispose();
        _db.Dispose();
    }

    [Fact]
    public async Task GetHome_NothingSaved_Returns204()
    {
        var response = await _factory.CreateClient().GetAsync("/display/home");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PutHome_Valid_SavesAndGetReturnsItSnakeCased()
    {
        var client = _factory.CreateClient();

        var put = await client.PutAsJsonAsync("/display/home",
            new { latitude = 41.88, longitude = -87.63, name = "Chicago", time_zone = "America/Chicago" });
        var json = await client.GetStringAsync("/display/home");

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.Contains("\"time_zone\":\"America/Chicago\"", json);
        Assert.Contains("\"latitude\":41.88", json);
    }

    [Theory]
    [InlineData(91.0, 0.0, null)]
    [InlineData(0.0, 181.0, null)]
    [InlineData(0.0, 0.0, "Not/AZone")]
    public async Task PutHome_Invalid_Returns400(double lat, double lon, string? zone)
    {
        var response = await _factory.CreateClient().PutAsJsonAsync("/display/home",
            new { latitude = lat, longitude = lon, time_zone = zone });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    public async Task Places_QueryTooShort_Returns400(string q)
    {
        var response = await _factory.CreateClient().GetAsync($"/display/places?q={q}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Preview_ZoneSaved_PassesItToTheRenderer()
    {
        var client = _factory.CreateClient();
        await client.PutAsJsonAsync("/display/home", new { time_zone = "Europe/London" });

        var response = await client.GetAsync("/display/preview?mode=daily");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("&tz=Europe%2FLondon", _renderer.LastRequestUri?.AbsoluteUri);
    }

    [Fact]
    public async Task Preview_NoZoneSaved_LeavesItToTheRenderersTz()
    {
        var response = await _factory.CreateClient().GetAsync("/display/preview?mode=daily");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("tz=", _renderer.LastRequestUri?.Query);
    }
}
