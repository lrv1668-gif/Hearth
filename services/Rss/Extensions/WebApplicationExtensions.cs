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
        app.MapGet("/rss/articles", async (RssStore store, RssFetcher fetcher, int count = 10) =>
        {
            if (store.IsStale())
            {
                try
                {
                    var articles = await fetcher.FetchAsync();
                    store.CacheArticles(articles);
                }
                catch (Exception ex)
                {
                    app.Logger.LogError(ex, "Failed to fetch RSS articles");
                }
            }

            return Results.Ok(store.GetArticles(count));
        });
    }
}
