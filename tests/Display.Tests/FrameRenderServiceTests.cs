using Display.Records;
using Xunit;

namespace Display.Tests;

/// <summary><see cref="FrameRenderService.Due"/> and <see cref="FrameRenderService.FramePath"/> — when and what the panel render captures.</summary>
public sealed class FrameRenderServiceTests
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

    private static readonly FrameSettings Saved =
        new("almanac", "inter", "landscape", [], [], 50, PhotoIntervalMinutes: 180);

    private static RenderedFrame Frame(string mode, string? photoId = null) =>
        new("\"abc\"", mode, photoId, Noon.AddHours(-1), Noon);

    [Fact]
    public void Due_NothingCached_IsNow()
    {
        Assert.Equal(DateTimeOffset.MinValue, FrameRenderService.Due(null, new(FrameModes.Daily, "default", null), Saved, null));
    }

    [Fact]
    public void Due_ModeMoved_IsNow()
    {
        var due = FrameRenderService.Due(Frame(FrameModes.Daily), new(FrameModes.Night, "schedule", null), Saved, null);
        Assert.Equal(DateTimeOffset.MinValue, due);
    }

    [Fact]
    public void Due_PinnedPhotoChanged_IsNow()
    {
        var due = FrameRenderService.Due(
            Frame(FrameModes.Photo, "a1"), new(FrameModes.Photo, "override", null, "b2"), Saved, null);
        Assert.Equal(DateTimeOffset.MinValue, due);
    }

    [Theory]
    [InlineData(FrameModes.Daily)]
    [InlineData(FrameModes.Countdown)]
    [InlineData(FrameModes.Night)]
    [InlineData(FrameModes.Note)]
    public void Due_FlatMode_FlatIntervalAfterTheLastRender(string mode)
    {
        var due = FrameRenderService.Due(Frame(mode), new(mode, "default", null), Saved, null);
        Assert.Equal(Noon + FrameRenderService.FlatInterval, due);
    }

    [Theory]
    [InlineData(FrameModes.Ambient)]
    [InlineData(FrameModes.Photo)]
    public void Due_PhotoMode_PhotoIntervalAfterTheLastRender(string mode)
    {
        var due = FrameRenderService.Due(Frame(mode), new(mode, "default", null), Saved, null);
        Assert.Equal(Noon.AddMinutes(180), due);
    }

    [Fact]
    public void Due_PhotoModeNothingSaved_UsesTheDefaultInterval()
    {
        var due = FrameRenderService.Due(Frame(FrameModes.Ambient), new(FrameModes.Ambient, "default", null), null, null);
        Assert.Equal(Noon.AddMinutes(FrameSettings.DefaultPhotoIntervalMinutes), due);
    }

    [Fact]
    public void Due_ModeEndsBeforeTheInterval_IsItsUntil()
    {
        var due = FrameRenderService.Due(
            Frame(FrameModes.Ambient), new(FrameModes.Ambient, "schedule", Noon.AddMinutes(20)), Saved, null);
        Assert.Equal(Noon.AddMinutes(20), due);
    }

    [Fact]
    public void Due_AfterAFailure_RetriesAfterTheRetryDelay()
    {
        var due = FrameRenderService.Due(
            Frame(FrameModes.Daily), new(FrameModes.Daily, "default", null), Saved, new(Noon, FrameModes.Daily, null));
        Assert.Equal(Noon + FrameRenderService.RetryDelay, due);
    }

    [Fact]
    public void Due_FailedWithNothingCached_RetriesSoon()
    {
        var due = FrameRenderService.Due(null, new(FrameModes.Ambient, "default", null), Saved, new(Noon, FrameModes.Ambient, null));
        Assert.Equal(Noon + FrameRenderService.FirstRetryDelay, due);
    }

    [Fact]
    public void Due_ModeMovedOnSinceAFailure_IsNow()
    {
        // A schedule boundary during a renderer outage still gets its attempt straight away
        var due = FrameRenderService.Due(
            Frame(FrameModes.Daily), new(FrameModes.Night, "schedule", null), Saved, new(Noon, FrameModes.Daily, null));
        Assert.Equal(DateTimeOffset.MinValue, due);
    }

    [Fact]
    public void FramePath_SpellsOutTheMode()
    {
        Assert.Equal("/frame?mode=daily", FrameRenderService.FramePath(new(FrameModes.Daily, "default", null)));
    }

    [Fact]
    public void FramePath_PinnedPhoto_AddsPin()
    {
        Assert.Equal(
            "/frame?mode=photo&pin=a%201",
            FrameRenderService.FramePath(new(FrameModes.Photo, "override", null, "a 1")));
    }
}
