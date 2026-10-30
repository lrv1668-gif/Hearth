using System.Globalization;
using Display.Records;
using Microsoft.AspNetCore.Http.Extensions;

namespace Display;

/// <summary>
/// Maps a /display/preview request onto a renderer call: which /frame URL to capture, at
/// what size, and how to dither it (HARDWARE.md → Display service).
/// </summary>
public static class FramePreview
{
    /// <summary>Preview-only params — everything else is passed on to /frame.</summary>
    private static readonly string[] OwnParams = ["look", "kernel", "dither"];

    /// <summary>
    /// The page size, resolved the way /frame does it: the URL's orientation and native
    /// panel size when valid, else the saved settings, else 1600×1200 landscape. Portrait
    /// swaps width and height.
    /// </summary>
    public static (int Width, int Height) Canvas(IQueryCollection query, FrameSettings? saved)
    {
        var orientation = query["orientation"].ToString();
        if (orientation is not ("landscape" or "portrait")) orientation = saved?.Orientation;
        var (width, height) = PanelSize(query)
            ?? (saved?.Width ?? FrameSettings.DefaultWidth, saved?.Height ?? FrameSettings.DefaultHeight);
        return orientation == "portrait" ? (height, width) : (width, height);
    }

    private static (int Width, int Height)? PanelSize(IQueryCollection query)
    {
        static int? Side(string? value) =>
            int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var n)
            && n is >= FrameSettings.MinSize and <= FrameSettings.MaxSize
                ? n
                : null;

        return (Side(query["width"]), Side(query["height"])) is ({ } w, { } h) && w >= h ? (w, h) : null;
    }

    /// <summary>
    /// The URL's <c>dither=</c> (ordered, errorDiffusion or none — the renderer rejects
    /// anything else), else the mode's method. <c>none</c> leaves the raw screenshot, e.g.
    /// for comparing inky's own dithering against Hearth's.
    /// </summary>
    public static string DitherFor(IQueryCollection query) =>
        query["dither"].FirstOrDefault() is { Length: > 0 } method ? method : DitherFor(query["mode"]);

    /// <summary>
    /// Ordered for the flat-UI modes, Daily, Countdown, Night and Note (tints become an even texture; the renderer still
    /// error-diffuses Daily's photo and album art); error diffusion for the photo modes, Ambient (also /frame's fallback)
    /// and Photo.
    /// </summary>
    public static string DitherFor(string? mode) =>
        mode is FrameModes.Daily or FrameModes.Countdown or FrameModes.Night or FrameModes.Note
            ? "ordered"
            : "errorDiffusion";

    /// <summary>/frame's <c>palette=</c>, which the renderer dithers to as well; Color unless it's B&amp;W.</summary>
    public static string PaletteFor(IQueryCollection query) =>
        query["palette"].ToString() == FramePalette.BlackAndWhite ? FramePalette.BlackAndWhite : FramePalette.Color;

    public static string FramePath(IQueryCollection query)
    {
        var frame = new QueryBuilder(query.Where(q => !OwnParams.Contains(q.Key)));
        return "/frame" + frame.ToQueryString();
    }
}
