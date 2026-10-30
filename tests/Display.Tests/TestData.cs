using Display.Records;

namespace Display.Tests;

/// <summary>The moment and the saved frame most Display tests start from.</summary>
internal static class TestData
{
    /// <summary>Monday 19 October 2026, 12:00 UTC.</summary>
    public static readonly DateTimeOffset Noon = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

    /// <summary>A landscape frame on the default look, with nothing in its columns.</summary>
    public static readonly FrameSettings Settings = new("almanac", "inter", "landscape", [], [], 50);
}
