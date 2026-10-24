namespace Rss.Records;

/// <summary>A parsed feed, as <see cref="RssFeeds"/> caches it — titles and links already sanitized.</summary>
public record Feed(string FeedTitle, List<ArticleItem> Articles);
