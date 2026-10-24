using System.Net;
using Display.Records;
using Hearth.TestUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Display.Tests;

public sealed class FramePreviewTests
{
    private static QueryCollection Query(params (string Key, string Value)[] pairs) =>
        new(pairs.ToDictionary(p => p.Key, p => new StringValues(p.Value)));

    private static readonly FrameSettings Saved =
        new("almanac", "inter", "portrait", [], [], 50, Width: 800, Height: 480);

    [Theory]
    [InlineData("landscape", 1600, 1200)]
    [InlineData("portrait", 1200, 1600)]
    public void Canvas_UrlSize_SwapsForPortrait(string orientation, int width, int height)
    {
        var query = Query(("orientation", orientation), ("width", "1600"), ("height", "1200"));
        Assert.Equal((width, height), FramePreview.Canvas(query, null));
    }

    [Fact]
    public void Canvas_NothingGiven_ReturnsDefaultLandscape()
    {
        Assert.Equal((1600, 1200), FramePreview.Canvas(Query(), null));
    }

    [Fact]
    public void Canvas_NoUrlSize_UsesSavedSizeAndOrientation()
    {
        Assert.Equal((480, 800), FramePreview.Canvas(Query(), Saved));
    }

    [Theory]
    [InlineData("1200", "1600")] // narrower than tall
    [InlineData("100", "80")] // below the minimum
    [InlineData("wide", "480")]
    public void Canvas_InvalidUrlSize_FallsBackToSaved(string width, string height)
    {
        var query = Query(("orientation", "landscape"), ("width", width), ("height", height));
        Assert.Equal((800, 480), FramePreview.Canvas(query, Saved));
    }

    [Theory]
    [InlineData("daily", "ordered")]
    [InlineData("countdown", "ordered")]
    [InlineData("night", "ordered")]
    [InlineData("note", "ordered")]
    [InlineData("ambient", "errorDiffusion")]
    [InlineData("photo", "errorDiffusion")]
    [InlineData(null, "errorDiffusion")]
    public void DitherFor_Mode_ReturnsMethod(string? mode, string method)
    {
        Assert.Equal(method, FramePreview.DitherFor(mode));
    }

    [Theory]
    [InlineData("none", "none")]
    [InlineData("errorDiffusion", "errorDiffusion")]
    [InlineData("", "ordered")]
    public void DitherFor_QueryDither_OverridesMode(string dither, string method)
    {
        Assert.Equal(method, FramePreview.DitherFor(Query(("mode", "daily"), ("dither", dither))));
    }

    [Fact]
    public void DitherFor_NoQueryDither_UsesMode()
    {
        Assert.Equal("ordered", FramePreview.DitherFor(Query(("mode", "daily"))));
    }

    [Fact]
    public void FramePath_DropsPreviewOnlyParams()
    {
        var query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["mode"] = "daily",
            ["left"] = "arc,today",
            ["look"] = "paper",
            ["kernel"] = "atkinson",
            ["dither"] = "none",
        });

        Assert.Equal("/frame?mode=daily&left=arc,today", FramePreview.FramePath(query));
    }

    [Fact]
    public async Task RenderAsync_Ok_SendsFramePathAndOptions()
    {
        var handler = new FakeHttpMessageHandler("png");
        var client = new RendererClient(new HttpClient(handler) { BaseAddress = new Uri("http://renderer:3000/") });

        await client.RenderAsync("/frame?mode=daily", 1600, 1200, "ordered", null, "paper");

        Assert.Equal(
            "http://renderer:3000/render?path=%2Fframe%3Fmode%3Ddaily&width=1600&height=1200&dither=ordered&look=paper",
            handler.LastRequestUri?.AbsoluteUri);
    }

    [Fact]
    public async Task RenderAsync_TimeZone_SendsItEscaped()
    {
        var handler = new FakeHttpMessageHandler("png");
        var client = new RendererClient(new HttpClient(handler) { BaseAddress = new Uri("http://renderer:3000/") });

        await client.RenderAsync("/frame", 1600, 1200, "ordered", null, "screen", "America/Chicago");

        Assert.EndsWith("&tz=America%2FChicago", handler.LastRequestUri?.AbsoluteUri);
    }

    [Fact]
    public async Task RenderAsync_RendererTimesOut_ThrowsWithStatus()
    {
        var handler = new FakeHttpMessageHandler("timeout", HttpStatusCode.GatewayTimeout);
        var client = new RendererClient(new HttpClient(handler) { BaseAddress = new Uri("http://renderer:3000/") });

        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.RenderAsync("/frame", 1600, 1200, "ordered", null, "screen"));
        Assert.Equal(HttpStatusCode.GatewayTimeout, ex.StatusCode);
    }
}
