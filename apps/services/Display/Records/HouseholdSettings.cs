namespace Display.Records;

/// <summary>An RSS feed the household follows: the dashboard's News Feed and the frame's Headlines.</summary>
public sealed record RssFeedSetting(string Title, string Url);

/// <summary>
/// A transit stop the household follows: the dashboard's Trains board and the frame's
/// Scheduled Departures. <paramref name="LineFilter"/> holds
/// <c>routeShortName|headsign|mode</c> keys; null shows every line, empty shows none.
/// </summary>
public sealed record TrainStopSetting(string Label, string StopKey, IReadOnlyList<string>? LineFilter = null);

/// <summary>
/// Lists every device shares (GET/PUT /display/household) — they used to live in each
/// browser's localStorage, which the frame's headless renderer never saw.
/// </summary>
public sealed record HouseholdSettings(IReadOnlyList<RssFeedSetting> RssFeeds, IReadOnlyList<TrainStopSetting> TrainStops)
{
    public const int MaxFeeds      = 50;
    public const int MaxStops      = 20;
    public const int MaxTextLength = 500;
    public const int MaxLines      = 200;

    /// <summary>Why the settings can't be saved, or null when they're valid.</summary>
    public string? Validate()
    {
        if (RssFeeds is null || TrainStops is null) return "rss_feeds and train_stops are required";
        if (RssFeeds.Count > MaxFeeds) return $"at most {MaxFeeds} feeds";
        if (TrainStops.Count > MaxStops) return $"at most {MaxStops} stops";

        foreach (var feed in RssFeeds)
        {
            if (feed is null || string.IsNullOrWhiteSpace(feed.Title) || feed.Title.Length > MaxTextLength)
                return "every feed needs a title";
            // Rss's FeedUrlValidator does the real checks (private hosts, DNS) when the feed is read.
            if (feed.Url is not { Length: <= MaxTextLength } ||
                !Uri.TryCreate(feed.Url, UriKind.Absolute, out var uri) ||
                uri.Scheme is not ("http" or "https"))
                return $"feed url must be http(s): {feed.Url}";
        }

        foreach (var stop in TrainStops)
        {
            if (stop is null || string.IsNullOrWhiteSpace(stop.StopKey) || stop.StopKey.Length > MaxTextLength)
                return "every stop needs a stop_key";
            if (string.IsNullOrWhiteSpace(stop.Label) || stop.Label.Length > MaxTextLength)
                return "every stop needs a label";
            if (stop.LineFilter is { } lines && (lines.Count > MaxLines || lines.Any(l => l is null || l.Length > MaxTextLength)))
                return "line_filter is too long";
        }

        return null;
    }
}
