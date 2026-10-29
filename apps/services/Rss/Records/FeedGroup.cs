namespace Rss.Records;

public record FeedGroup(string FeedTitle, string FeedUrl, IEnumerable<ArticleItem> Articles);
