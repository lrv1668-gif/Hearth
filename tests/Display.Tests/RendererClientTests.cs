using System.Net;
using Hearth.TestUtilities;

namespace Display.Tests;

/// <summary>What <see cref="RendererClient"/> asks the renderer sidecar for.</summary>
public sealed class RendererClientTests
{
    private readonly FakeHttpMessageHandler _renderer = new("png");

    private RendererClient Client() => new(new HttpClient(_renderer) { BaseAddress = new Uri("http://renderer:3000/") });

    [Fact]
    public async Task RenderAsync_BlackAndWhite_SendsThePalette()
    {
        await Client().RenderAsync("/frame", 1600, 1200, "ordered", null, "screen", palette: "bw");

        Assert.EndsWith("&palette=bw", _renderer.LastRequestUri?.AbsoluteUri);
    }

    [Fact]
    public async Task RenderAsync_Ok_SendsFramePathAndOptions()
    {
        await Client().RenderAsync("/frame?mode=daily", 1600, 1200, "ordered", null, "paper");

        Assert.Equal(
            "http://renderer:3000/render?path=%2Fframe%3Fmode%3Ddaily&width=1600&height=1200&dither=ordered&look=paper",
            _renderer.LastRequestUri?.AbsoluteUri);
    }

    [Fact]
    public async Task RenderAsync_TimeZone_SendsItEscaped()
    {
        await Client().RenderAsync("/frame", 1600, 1200, "ordered", null, "screen", "America/Chicago");

        Assert.EndsWith("&tz=America%2FChicago", _renderer.LastRequestUri?.AbsoluteUri);
    }

    [Fact]
    public async Task RenderAsync_RendererTimesOut_ThrowsWithStatus()
    {
        _renderer.Status = HttpStatusCode.GatewayTimeout;
        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => Client().RenderAsync("/frame", 1600, 1200, "ordered", null, "screen"));
        Assert.Equal(HttpStatusCode.GatewayTimeout, ex.StatusCode);
    }
}
