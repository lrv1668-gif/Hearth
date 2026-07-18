using Rss.Records;
using Rss.Tests.Helpers;
using Xunit;

namespace Rss.Tests;

public sealed class RssStoreTests
{
    private const string Url = "https://example.com/feed";

    private static RssStore Migrated(TempDatabase tmp)
    {
        var store = new RssStore(tmp.Db);
        store.Migrate();
        return store;
    }

    private static ArticleItem Article(string title, string publishedAt) =>
        new(title, $"https://example.com/{title}", $"desc {title}", publishedAt);

    [Fact]
    public void CacheArticles_ThenGet_RoundTripsArticlesAndTitle()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        store.CacheArticles(Url, "My Feed", new[] { Article("a", "2026-06-01") });

        Assert.Equal("My Feed", store.GetFeedTitle(Url));
        var article = Assert.Single(store.GetArticles(Url, 10));
        Assert.Equal("a", article.Title);
        Assert.Equal("desc a", article.Description);
    }

    [Fact]
    public void GetArticles_OrdersByPublishedAtDescending()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        store.CacheArticles(Url, "Feed", new[]
        {
            Article("oldest", "2026-06-01"),
            Article("newest", "2026-06-03"),
            Article("middle", "2026-06-02"),
        });

        var titles = store.GetArticles(Url, 10).Select(a => a.Title).ToArray();
        Assert.Equal(new[] { "newest", "middle", "oldest" }, titles);
    }

    [Fact]
    public void GetArticles_RespectsCountLimit()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        store.CacheArticles(Url, "Feed", new[]
        {
            Article("a", "2026-06-01"),
            Article("b", "2026-06-02"),
            Article("c", "2026-06-03"),
        });

        Assert.Equal(2, store.GetArticles(Url, 2).Count());
    }

    [Fact]
    public void CacheArticles_CalledAgain_ReplacesPreviousArticles()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        store.CacheArticles(Url, "Feed", new[] { Article("old", "2026-06-01") });
        store.CacheArticles(Url, "Feed", new[] { Article("new", "2026-06-02") });

        var article = Assert.Single(store.GetArticles(Url, 10));
        Assert.Equal("new", article.Title);
    }

    [Fact]
    public void IsStale_NoCachedFeed_ReturnsTrue()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        Assert.True(store.IsStale(Url));
    }

    [Fact]
    public void IsStale_FreshlyCachedFeed_ReturnsFalse()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        store.CacheArticles(Url, "Feed", new[] { Article("a", "2026-06-01") });

        Assert.False(store.IsStale(Url));
    }
}
