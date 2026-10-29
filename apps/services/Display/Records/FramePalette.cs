using System.Text.Json.Serialization;

namespace Display.Records;

/// <summary>
/// The frame's Color ↔ B&amp;W mode — the ids /frame?palette= takes. Kept apart from
/// <see cref="FrameSettings"/> (saved whole on every Settings change) so the frame's
/// Button C can flip it without a Settings tab putting it back.
/// </summary>
public sealed record FramePalette(string Palette)
{
    public const string Color = "color";
    public const string BlackAndWhite = "bw";

    public static readonly string[] All = [Color, BlackAndWhite];

    public static readonly FramePalette Default = new(Color);

    [JsonIgnore] public bool IsBlackAndWhite => Palette == BlackAndWhite;

    /// <summary>The other palette.</summary>
    public FramePalette Toggled() => new(IsBlackAndWhite ? Color : BlackAndWhite);

    /// <summary>Why the palette can't be saved, or null when it's valid.</summary>
    public string? Validate() =>
        All.Contains(Palette) ? null : $"palette must be one of {string.Join(", ", All)}";
}
