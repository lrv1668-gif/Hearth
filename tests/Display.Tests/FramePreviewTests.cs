using Display.Records;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Display.Tests;

public sealed class FramePreviewTests
{
    private static QueryCollection Query(params (string Key, string Value)[] pairs) =>
        new(pairs.ToDictionary(p => p.Key, p => new StringValues(p.Value)));

    private static readonly FrameSettings Saved =
        TestData.Settings with { Orientation = "portrait", Width = 800, Height = 480 };

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

    [Theory]
    [InlineData("bw", "bw")]
    [InlineData("color", "color")]
    [InlineData("", "color")]
    [InlineData("sepia", "color")]
    public void PaletteFor_Query_ReturnsPalette(string palette, string expected)
    {
        Assert.Equal(expected, FramePreview.PaletteFor(Query(("mode", "daily"), ("palette", palette))));
    }

    [Fact]
    public void FramePath_KeepsThePalette()
    {
        Assert.Equal("/frame?mode=daily&palette=bw", FramePreview.FramePath(Query(("mode", "daily"), ("palette", "bw"))));
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
}
