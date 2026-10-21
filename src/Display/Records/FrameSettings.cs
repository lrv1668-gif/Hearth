namespace Display.Records;

/// <summary>
/// The e-paper frame's look, Daily layout and Ambient photos — one shared record for
/// every client, since the frame is a single physical display (the browser /ambient
/// slideshow reads the photo choices too). Theme, font, widget and category ids are
/// the frontend's (constants/frameThemes.ts, fontThemes.ts, frameWidgets.ts,
/// ambient.ts); the frontend drops unknown ids when it reads them.
/// </summary>
public sealed record FrameSettings(
    string Theme,
    string Font,
    string Orientation,
    IReadOnlyList<string> Left,
    IReadOnlyList<string> Right,
    int Split,
    // Where Ambient mode's info card sits. Defaulted so older payloads and rows still read.
    string AmbientPosition = FrameSettings.DefaultAmbientPosition,
    // Ambient photos: "unsplash", "local" (uploads), "both", or another Photos-service
    // source, plus the Unsplash categories to draw from. Defaulted like AmbientPosition.
    string PhotoSource = FrameSettings.DefaultPhotoSource,
    IReadOnlyList<string>? PhotoCategories = null
)
{
    public const string DefaultPhotoSource = "unsplash";
    public const int MaxPhotoCategories = 20;

    public const string DefaultAmbientPosition = "bottom-left";
    public static readonly string[] AmbientPositions =
        ["top-left", "top-middle", "top-right", "bottom-left", "bottom-middle", "bottom-right"];

    public const int MaxColumnWidgets = 3;
    public const int MinSplit = 30;
    public const int MaxSplit = 70;

    /// <summary>Why the settings can't be saved, or null when they're valid.</summary>
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Theme)) return "theme required";
        if (string.IsNullOrWhiteSpace(Font)) return "font required";
        if (Orientation is not ("landscape" or "portrait")) return "orientation must be landscape or portrait";
        if (Left is null || Right is null) return "left and right columns required";
        if (Left.Count > MaxColumnWidgets || Right.Count > MaxColumnWidgets)
            return $"at most {MaxColumnWidgets} widgets per column";
        var all = Left.Concat(Right).ToList();
        if (all.Any(string.IsNullOrWhiteSpace)) return "widget ids must not be blank";
        if (all.Distinct().Count() != all.Count) return "a widget can appear only once";
        if (Split is < MinSplit or > MaxSplit) return $"split must be {MinSplit}–{MaxSplit}";
        if (!AmbientPositions.Contains(AmbientPosition))
            return $"ambient_position must be one of {string.Join(", ", AmbientPositions)}";
        if (string.IsNullOrWhiteSpace(PhotoSource) || PhotoSource.Length > 40) return "photo_source required";
        if (PhotoCategories is { } categories &&
            (categories.Count > MaxPhotoCategories || categories.Any(string.IsNullOrWhiteSpace)))
            return $"at most {MaxPhotoCategories} photo categories, none blank";
        return null;
    }
}
