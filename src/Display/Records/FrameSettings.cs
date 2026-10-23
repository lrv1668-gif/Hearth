namespace Display.Records;

/// <summary>
/// The e-paper frame's look, Daily layout, Ambient photos and per-mode choices — one shared record for
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
    IReadOnlyList<string>? PhotoCategories = null,
    // The panel's native (landscape) size in pixels; portrait swaps it. Defaulted to
    // the Inky Impression 13.3" so older payloads and rows still read.
    int Width = FrameSettings.DefaultWidth,
    int Height = FrameSettings.DefaultHeight,
    // Photo mode: the uploads to show (one pins it, several make an album) and how much
    // of Ambient's info card to keep — "full", "caption" or "none".
    IReadOnlyList<string>? PhotoIds = null,
    string PhotoCard = FrameSettings.DefaultPhotoCard,
    // Countdown mode: the countdown task to show; null shows the next one coming up.
    int? CountdownId = null,
    // Note mode: your own text — a heading and a body, one line per rule or item.
    string? NoteTitle = null,
    string? NoteBody = null
)
{
    public const string DefaultPhotoSource = "unsplash";
    public const int MaxPhotoCategories = 20;

    public const string DefaultAmbientPosition = "bottom-left";
    public static readonly string[] AmbientPositions =
        ["top-left", "top-middle", "top-right", "bottom-left", "bottom-middle", "bottom-right"];

    public const string DefaultPhotoCard = "full";
    public static readonly string[] PhotoCards = ["full", "caption", "none"];
    public const int MaxPhotoIds = 50;

    public const int MaxNoteTitle = 80;
    public const int MaxNoteBody = 600;
    public const int MaxNoteLines = 12;

    public const int DefaultWidth = 1600;
    public const int DefaultHeight = 1200;
    public const int MinSize = 200;
    public const int MaxSize = 4000;

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
        if (Width is < MinSize or > MaxSize || Height is < MinSize or > MaxSize)
            return $"width and height must be {MinSize}–{MaxSize}";
        if (PhotoIds is { } ids && (ids.Count > MaxPhotoIds || ids.Any(string.IsNullOrWhiteSpace)))
            return $"at most {MaxPhotoIds} photo ids, none blank";
        if (!PhotoCards.Contains(PhotoCard)) return $"photo_card must be one of {string.Join(", ", PhotoCards)}";
        if (NoteTitle is { Length: > MaxNoteTitle }) return $"note_title must be at most {MaxNoteTitle} characters";
        if (NoteBody is { Length: > MaxNoteBody }) return $"note_body must be at most {MaxNoteBody} characters";
        if (NoteBody is not null && NoteBody.Split('\n').Length > MaxNoteLines)
            return $"note_body must be at most {MaxNoteLines} lines";
        if (Width < Height) return "width must be ≥ height (the panel's native landscape size)";
        return null;
    }
}
