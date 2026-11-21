using System.Text.RegularExpressions;
using Display.Records;

namespace Display.Tests;

/// <summary>
/// The frontend repeats Display's settings limits in constants/frameLimits.ts (for its
/// sliders, inputs and URL parsing); this fails when a value there drifts from its twin here.
/// </summary>
public sealed partial class FrontendLimitsTests
{
    public static TheoryData<string, int> Limits => new()
    {
        { "DEFAULT_FRAME_TEXT_SIZE",        FrameSettings.DefaultTextSize },
        { "MIN_FRAME_TEXT_SIZE",            FrameSettings.MinTextSize },
        { "MAX_FRAME_TEXT_SIZE",            FrameSettings.MaxTextSize },
        { "MIN_FRAME_SIZE",                 FrameSettings.MinSize },
        { "MAX_FRAME_SIZE",                 FrameSettings.MaxSize },
        { "MAX_COLUMN_WIDGETS",             FrameSettings.MaxColumnWidgets },
        { "MIN_SPLIT",                      FrameSettings.MinSplit },
        { "MAX_SPLIT",                      FrameSettings.MaxSplit },
        { "DEFAULT_PHOTO_INTERVAL_MINUTES", FrameSettings.DefaultPhotoIntervalMinutes },
        { "MIN_PHOTO_INTERVAL_MINUTES",     FrameSettings.MinPhotoIntervalMinutes },
        { "MAX_PHOTO_INTERVAL_MINUTES",     FrameSettings.MaxPhotoIntervalMinutes },
        { "MAX_NOTE_TITLE",                 FrameSettings.MaxNoteTitle },
        { "MAX_NOTE_BODY",                  FrameSettings.MaxNoteBody },
        { "MAX_NOTE_LINES",                 FrameSettings.MaxNoteLines },
        { "MAX_RULE_MODES",                 ModeRule.MaxRotate + 1 },
    };

    private static readonly Dictionary<string, int> Frontend = ConstLine()
        .Matches(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "frameLimits.ts")))
        .ToDictionary(m => m.Groups[1].Value, m => int.Parse(m.Groups[2].Value));

    [Theory]
    [MemberData(nameof(Limits))]
    public void FrameLimits_MatchDisplay(string name, int expected)
    {
        Assert.True(Frontend.TryGetValue(name, out var actual), $"{name} missing from frameLimits.ts");
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FrameLimits_EveryConstantIsChecked()
    {
        var checkedNames = Limits.Select(row => (string)row[0]).ToHashSet();
        Assert.Empty(Frontend.Keys.Where(name => !checkedNames.Contains(name)));
    }

    [GeneratedRegex(@"^export const (\w+) = (\d+);", RegexOptions.Multiline)]
    private static partial Regex ConstLine();
}
