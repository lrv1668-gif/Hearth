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

    [Theory]
    [InlineData(FrameSettings.MinPhotoIntervalMinutes - 1)]
    [InlineData(FrameSettings.MaxPhotoIntervalMinutes + 1)]
    public void Validate_PhotoIntervalOutOfRange_ReturnsError(int minutes)
    {
        Assert.NotNull((Valid with { PhotoIntervalMinutes = minutes }).Validate());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(180)]
    public void Validate_PortraitRotationNotAQuarterTurn_ReturnsError(int degrees)
    {
        Assert.NotNull((Valid with { PortraitRotation = degrees }).Validate());
    }

    [Fact]
    public void Validate_CounterClockwisePortrait_ReturnsNull()
    {
        Assert.Null((Valid with { PortraitRotation = 270, PhotoIntervalMinutes = 360 }).Validate());
    }

    [Fact]
    public void Validate_UnknownPhotoCard_ReturnsError()
    {
        Assert.NotNull((Valid with { PhotoCard = "poster" }).Validate());
    }

    [Fact]
    public void Validate_BlankPhotoId_ReturnsError()
    {
        Assert.NotNull((Valid with { PhotoIds = ["abc", " "] }).Validate());
    }

    [Fact]
    public void Validate_TooManyPhotoIds_ReturnsError()
    {
        var ids = Enumerable.Range(0, FrameSettings.MaxPhotoIds + 1).Select(i => $"p{i}").ToList();
        Assert.NotNull((Valid with { PhotoIds = ids }).Validate());
    }

    [Fact]
    public void Validate_NoteWithinLimits_ReturnsNull()
    {
        var body = string.Join('\n', Enumerable.Repeat("No shoes inside", FrameSettings.MaxNoteLines));
        Assert.Null((Valid with { NoteTitle = "House rules", NoteBody = body }).Validate());
    }

    [Fact]
    public void Validate_NoteTooLong_ReturnsError()
    {
        Assert.NotNull((Valid with { NoteTitle = new string('a', FrameSettings.MaxNoteTitle + 1) }).Validate());
        Assert.NotNull((Valid with { NoteBody = new string('a', FrameSettings.MaxNoteBody + 1) }).Validate());
        Assert.NotNull((Valid with { NoteBody = string.Join('\n', Enumerable.Repeat("x", FrameSettings.MaxNoteLines + 1)) }).Validate());
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
        Assert.Equal((1600, 1200), (settings.Width, settings.Height));
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

    [Theory]
    [InlineData(199, 199)]
    [InlineData(4001, 1200)]
    public void Validate_SizeOutOfRange_ReturnsError(int width, int height)
    {
        Assert.NotNull((Valid with { Width = width, Height = height }).Validate());
    }

    [Fact]
    public void Validate_HeightAboveWidth_ReturnsError()
    {
        Assert.NotNull((Valid with { Width = 480, Height = 800 }).Validate());
    }

    [Fact]
    public void Validate_CustomLandscapeSize_ReturnsNull()
    {
        Assert.Null((Valid with { Width = 800, Height = 480 }).Validate());
    }
}
