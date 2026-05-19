using Rss.Records;

namespace Rss.Extensions;

public static class WebApplicationExtensions
{
    public static void InitializeWebAppForRss(this WebApplication app)
    {
        app.Services.GetRequiredService<RssStore>().Migrate();
        app.AddRssEndpoints();
    }

    private static void AddRssEndpoints(this WebApplication app)
    {
        app.MapGet("/rss/articles", async (RssStore store, RssFetcher fetcher, string[]? url, int count = 10) =>
        {
            if (url is null || url.Length == 0)
                return Results.Ok(Array.Empty<FeedGroup>());

            var groups = new List<FeedGroup>();

            foreach (var feedUrl in url)
            {
                if (store.IsStale(feedUrl))
                {
                    var result = await fetcher.FetchAsync(feedUrl);
                    if (result is not null)
                        store.CacheArticles(feedUrl, result.Value.FeedTitle, result.Value.Articles);
                    else
                        app.Logger.LogError("Failed to fetch RSS feed: {Url}", feedUrl);
                }

                var articles = store.GetArticles(feedUrl, count).ToList();
                if (articles.Count > 0)
                {
                    var feedTitle = store.GetFeedTitle(feedUrl) ?? feedUrl;
                    groups.Add(new FeedGroup(feedTitle, feedUrl, articles));
                }
            }

            return Results.Ok(groups);
        });
    }
}
