namespace Rss.Records;

/// <summary>One story on the frame's Headlines, with the feed it came from.</summary>
/// <param name="PublishedAt">ISO 8601 in UTC, or null when the feed gives no date.</param>
public record Headline(string Title, string FeedTitle, string? PublishedAt);
