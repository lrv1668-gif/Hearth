using ServiceDefaults;
using Data.Abstractions;
using Rss.Records;

namespace Rss.Extensions;

public static class WebApplicationExtensions
{
    public static void InitializeWebAppForRss(this WebApplication app)
    {
        // Articles used to be cached in their own table; FusionCache's L2 holds them now
        app.Services.GetRequiredKeyedService<IDatabase>("rss").NonQuery("DROP TABLE IF EXISTS rss_articles");
        app.AddRssEndpoints();
        app.MapHearthHealth("rss");
    }

    private static void AddRssEndpoints(this WebApplication app)
    {
        app.MapGet("/rss/articles", async (
            RssFeeds feeds, FeedUrlValidator validator, string[]? url, CancellationToken ct, int count = 10) =>
        {
            if (url is null || url.Length == 0)
                return Results.Ok(Array.Empty<FeedGroup>());

            var pinned = await Task.WhenAll(url.Select(validator.ResolvePinnedAddressAsync));
            for (var i = 0; i < url.Length; i++)
            {
                if (pinned[i] is not null) continue;
                app.Logger.LogError("Rejected disallowed RSS feed URL: {Url}", url[i]);
                return Results.BadRequest(new { error = $"invalid or disallowed feed url: {url[i]}" });
            }

            // Every feed at once: a slow one no longer delays the rest
            var fetched = await Task.WhenAll(url.Select((feedUrl, i) => feeds.GetAsync(feedUrl, pinned[i]!, ct)));

            var groups = url.Zip(fetched)
                .Where(f => f.Second is { Articles.Count: > 0 })
                .Select(f => new FeedGroup(
                    f.Second!.FeedTitle,
                    f.First,
                    f.Second.Articles
                        .OrderByDescending(a => a.PublishedAt, StringComparer.Ordinal)
                        .Take(count)
                        .ToList()))
                .ToList();

            return Results.Ok(groups);
        });
    }
}
