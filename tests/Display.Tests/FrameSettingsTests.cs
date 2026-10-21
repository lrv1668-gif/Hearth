using Display.Records;
using Xunit;

namespace Display.Tests;

public sealed class FrameSettingsTests
{
    private static readonly FrameSettings Valid =
        new("almanac", "inter", "portrait", ["arc", "today", "moon"], [], 30);

    [Fact]
    public void Validate_WellFormed_ReturnsNull()
    {
        Assert.Null(Valid.Validate());
    }

    [Fact]
    public void Validate_UnknownOrientation_ReturnsError()
    {
        Assert.NotNull((Valid with { Orientation = "sideways" }).Validate());
    }

    [Fact]
    public void Validate_TooManyWidgetsInAColumn_ReturnsError()
    {
        Assert.NotNull((Valid with { Right = ["a", "b", "c", "d"] }).Validate());
    }

    [Fact]
    public void Validate_WidgetInBothColumns_ReturnsError()
    {
        Assert.NotNull((Valid with { Right = ["today"] }).Validate());
    }

    [Theory]
    [InlineData(29)]
    [InlineData(71)]
    public void Validate_SplitOutOfRange_ReturnsError(int split)
    {
        Assert.NotNull((Valid with { Split = split }).Validate());
    }

    [Fact]
    public void Validate_UnknownAmbientPosition_ReturnsError()
    {
        Assert.NotNull((Valid with { AmbientPosition = "middle" }).Validate());
    }

    [Fact]
    public void Deserialize_WithoutAmbientPosition_DefaultsToBottomLeft()
    {
        var options = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower };
        var json = """{"theme":"almanac","font":"inter","orientation":"landscape","left":[],"right":[],"split":50}""";

        var settings = System.Text.Json.JsonSerializer.Deserialize<FrameSettings>(json, options)!;

        Assert.Equal("bottom-left", settings.AmbientPosition);
        Assert.Equal("unsplash", settings.PhotoSource);
        Assert.Null(settings.Validate());
    }

    [Fact]
    public void Validate_BlankPhotoSource_ReturnsError()
    {
        Assert.NotNull((Valid with { PhotoSource = "" }).Validate());
    }

    [Fact]
    public void Validate_BlankPhotoCategory_ReturnsError()
    {
        Assert.NotNull((Valid with { PhotoCategories = ["nature", " "] }).Validate());
    }

    [Fact]
    public void Validate_BlankTheme_ReturnsError()
    {
        Assert.NotNull((Valid with { Theme = " " }).Validate());
    }
}
