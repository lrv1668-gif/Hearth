using Display.Records;

namespace Display.Tests;

/// <summary><see cref="FrameRenderService.Due"/> and <see cref="FrameRenderService.FramePath"/> — when and what the panel render captures.</summary>
public sealed class FrameRenderServiceTests
{
    private static readonly DateTimeOffset Noon = TestData.Noon;

    private static readonly FrameSettings Saved = TestData.Settings with { PhotoIntervalMinutes = 180 };

    private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;

    private static RenderedFrame Frame(string mode, string? photoId = null) =>
        new("\"abc\"", mode, photoId, Noon.AddHours(-1), Noon);

    [Fact]
    public void Due_NothingCached_IsNow()
    {
        Assert.Equal(DateTimeOffset.MinValue, FrameRenderService.Due(null, new(FrameModes.Daily, "default", null), Saved, null, Noon, Utc));
    }

    [Fact]
    public void Due_ModeMoved_IsNow()
    {
        var due = FrameRenderService.Due(Frame(FrameModes.Daily), new(FrameModes.Night, "schedule", null), Saved, null, Noon, Utc);
        Assert.Equal(DateTimeOffset.MinValue, due);
    }

    [Fact]
    public void Due_PinnedPhotoChanged_IsNow()
    {
        var due = FrameRenderService.Due(
            Frame(FrameModes.Photo, "a1"), new(FrameModes.Photo, "override", null, "b2"), Saved, null, Noon, Utc);
        Assert.Equal(DateTimeOffset.MinValue, due);
    }

    [Theory]
    [InlineData(FrameModes.Daily)]
    [InlineData(FrameModes.Countdown)]
    [InlineData(FrameModes.Night)]
    [InlineData(FrameModes.Note)]
    public void Due_FlatMode_FlatIntervalAfterTheLastRender(string mode)
    {
        var due = FrameRenderService.Due(Frame(mode), new(mode, "default", null), Saved, null, Noon, Utc);
        Assert.Equal(Noon + FrameRenderService.FlatInterval, due);
    }

    [Theory]
    [InlineData(FrameModes.Ambient)]
    [InlineData(FrameModes.Photo)]
    public void Due_PhotoMode_PhotoIntervalAfterTheLastRender(string mode)
    {
        var due = FrameRenderService.Due(Frame(mode), new(mode, "default", null), Saved, null, Noon, Utc);
        Assert.Equal(Noon.AddMinutes(180), due);
    }

    [Fact]
    public void Due_PhotoModeNothingSaved_UsesTheDefaultInterval()
    {
        var due = FrameRenderService.Due(Frame(FrameModes.Ambient), new(FrameModes.Ambient, "default", null), null, null, Noon, Utc);
        Assert.Equal(Noon.AddMinutes(FrameSettings.DefaultPhotoIntervalMinutes), due);
    }

    [Fact]
    public void Due_ModeEndsBeforeTheInterval_IsItsUntil()
    {
        var due = FrameRenderService.Due(
            Frame(FrameModes.Ambient), new(FrameModes.Ambient, "schedule", Noon.AddMinutes(20)), Saved, null, Noon, Utc);
        Assert.Equal(Noon.AddMinutes(20), due);
    }

    [Fact]
    public void Due_AfterAFailure_RetriesAfterTheRetryDelay()
    {
        var due = FrameRenderService.Due(
            Frame(FrameModes.Daily), new(FrameModes.Daily, "default", null), Saved, new(Noon, FrameModes.Daily, null), Noon, Utc);
        Assert.Equal(Noon + FrameRenderService.RetryDelay, due);
    }

    [Fact]
    public void Due_FailedWithNothingCached_RetriesSoon()
    {
        var due = FrameRenderService.Due(null, new(FrameModes.Ambient, "default", null), Saved, new(Noon, FrameModes.Ambient, null), Noon, Utc);
        Assert.Equal(Noon + FrameRenderService.FirstRetryDelay, due);
    }

    [Fact]
    public void Due_ModeMovedOnSinceAFailure_IsNow()
    {
        // A schedule boundary during a renderer outage still gets its attempt straight away
        var due = FrameRenderService.Due(
            Frame(FrameModes.Daily), new(FrameModes.Night, "schedule", null), Saved, new(Noon, FrameModes.Daily, null), Noon, Utc);
        Assert.Equal(DateTimeOffset.MinValue, due);
    }

    // Saved with quiet hours 23:00–06:00 (UTC)
    private static readonly FrameSettings Quiet = Saved with { QuietStart = "23:00", QuietEnd = "06:00" };

    private static RenderedFrame FrameAt(string mode, DateTimeOffset checkedAt) =>
        new("\"abc\"", mode, null, checkedAt, checkedAt);

    [Fact]
    public void Due_IntervalLandsInQuietHours_WaitsForThemToEnd()
    {
        var lastRender = Noon.AddHours(10).AddMinutes(50);
        var due = FrameRenderService.Due(
            FrameAt(FrameModes.Daily, lastRender), new(FrameModes.Daily, "default", null), Quiet, null, lastRender, Utc);
        Assert.Equal(Noon.AddHours(18), due);
    }

    [Fact]
    public void Due_IntervalBeforeQuietHours_IsUnchanged()
    {
        var due = FrameRenderService.Due(Frame(FrameModes.Daily), new(FrameModes.Daily, "default", null), Quiet, null, Noon, Utc);
        Assert.Equal(Noon + FrameRenderService.FlatInterval, due);
    }

    [Fact]
    public void Due_OverdueDuringQuietHours_WaitsForThemToEnd()
    {
        // Host was down since yesterday afternoon and comes back at 02:00: no redraw until 06:00
        var now = Noon.AddHours(14);
        var due = FrameRenderService.Due(Frame(FrameModes.Ambient), new(FrameModes.Ambient, "default", null), Quiet, null, now, Utc);
        Assert.Equal(Noon.AddHours(18), due);
    }

    [Fact]
    public void Due_ModeMovedDuringQuietHours_IsNow()
    {
        var now = Noon.AddHours(11);
        var due = FrameRenderService.Due(Frame(FrameModes.Daily), new(FrameModes.Night, "schedule", null), Quiet, null, now, Utc);
        Assert.Equal(DateTimeOffset.MinValue, due);
    }

    [Fact]
    public void Due_ModeEndsDuringQuietHours_IsItsUntil()
    {
        // The schedule may change the mode inside the window, and that still draws
        var now = Noon.AddHours(11);
        var until = now.AddHours(1);
        var due = FrameRenderService.Due(
            FrameAt(FrameModes.Daily, now), new(FrameModes.Daily, "schedule", until), Quiet, null, now, Utc);
        Assert.Equal(until, due);
    }

    [Fact]
    public void FramePath_SpellsOutTheMode()
    {
        Assert.Equal("/frame?mode=daily", FrameRenderService.FramePath(new(FrameModes.Daily, "default", null)));
    }

    [Theory]
    [InlineData("hearth", FrameModes.Daily, "ordered")]
    [InlineData("hearth", FrameModes.Ambient, "errorDiffusion")]
    [InlineData("frame", FrameModes.Daily, "none")]
    [InlineData("frame", FrameModes.Ambient, "none")]
    public void DitherFor_Setting_PicksMethod(string dithering, string mode, string method)
    {
        Assert.Equal(method, FrameRenderService.DitherFor(new(mode, "default", null), Saved with { Dithering = dithering }));
    }

    [Theory]
    [InlineData("hearth", FrameModes.Daily, "ordered")]
    [InlineData("frame", FrameModes.Daily, "ordered")]
    [InlineData("frame", FrameModes.Ambient, "errorDiffusion")]
    public void DitherFor_BlackAndWhite_HearthAlwaysDithers(string dithering, string mode, string method)
    {
        Assert.Equal(method, FrameRenderService.DitherFor(
            new(mode, "default", null), Saved with { Dithering = dithering }, new(FramePalette.BlackAndWhite)));
    }

    [Fact]
    public void FramePath_BlackAndWhite_AddsThePalette()
    {
        Assert.Equal("/frame?mode=daily&palette=bw",
            FrameRenderService.FramePath(new(FrameModes.Daily, "default", null), palette: new(FramePalette.BlackAndWhite)));
    }

    [Fact]
    public void FramePath_Color_LeavesThePaletteOut()
    {
        Assert.Equal("/frame?mode=daily",
            FrameRenderService.FramePath(new(FrameModes.Daily, "default", null), palette: FramePalette.Default));
    }

    [Fact]
    public void DitherFor_NothingSaved_UsesTheModesMethod()
    {
        Assert.Equal("ordered", FrameRenderService.DitherFor(new(FrameModes.Daily, "default", null), null));
    }

    [Fact]
    public void FramePath_PinnedPhoto_PassesOnlyThePhoto()
    {
        Assert.Equal("/frame?mode=photo", FrameRenderService.FramePath(new(FrameModes.Photo, "override", null, "a 1")));
    }

    [Fact]
    public void FramePath_HeldPhoto_PassesItAsJson()
    {
        var photo = new FramePhoto("u1", "/photos/files/u1.jpg", null, "Lake", null, null, "local");
        var path = Uri.UnescapeDataString(FrameRenderService.FramePath(new(FrameModes.Ambient, "default", null), photo));

        Assert.StartsWith("/frame?mode=ambient&photo=", path);
        Assert.Contains("\"url\":\"/photos/files/u1.jpg\"", path);
        Assert.Contains("\"source\":\"local\"", path);
    }

    [Fact]
    public void NextPhoto_IntervalRender_MovesOn()
    {
        Assert.True(FrameRenderService.NextPhoto(Frame(FrameModes.Ambient), new(FrameModes.Ambient, "default", null), false, false, null));
    }

    [Fact]
    public void NextPhoto_SettingsChange_KeepsThePhoto()
    {
        Assert.False(FrameRenderService.NextPhoto(Frame(FrameModes.Ambient), new(FrameModes.Ambient, "default", null), true, false, null));
    }

    [Fact]
    public void NextPhoto_ModeChanged_KeepsThePhoto()
    {
        Assert.False(FrameRenderService.NextPhoto(Frame(FrameModes.Daily), new(FrameModes.Ambient, "schedule", null), false, false, null));
    }

    [Fact]
    public void NextPhoto_Retry_KeepsThePhoto()
    {
        var failure = new FrameRenderService.RenderFailure(Noon, FrameModes.Ambient, null);
        Assert.False(FrameRenderService.NextPhoto(Frame(FrameModes.Ambient), new(FrameModes.Ambient, "default", null), false, false, failure));
    }

    [Fact]
    public void PhotoMaxAge_EnteringThePhotoMode_IsThePhotoInterval()
    {
        Assert.Equal(TimeSpan.FromMinutes(180), FrameRenderService.PhotoMaxAge(Frame(FrameModes.Daily), new(FrameModes.Ambient, "schedule", null), Saved));
    }

    [Fact]
    public void PhotoMaxAge_StayingInTheMode_IsNone()
    {
        Assert.Null(FrameRenderService.PhotoMaxAge(Frame(FrameModes.Ambient), new(FrameModes.Ambient, "default", null), Saved));
    }

    [Fact]
    public void NextPhoto_Refresh_MovesOn()
    {
        Assert.True(FrameRenderService.NextPhoto(Frame(FrameModes.Ambient), new(FrameModes.Ambient, "default", null), true, true, null));
    }
}
