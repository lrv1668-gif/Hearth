using System.Data.Common;
using Data.Abstractions;
using Rss.Records;

namespace Rss;

public sealed class RssStore([FromKeyedServices("rss")] IDatabase db)
{
    public void Migrate() => db.NonQuery("""
        CREATE TABLE IF NOT EXISTS rss_articles (
            id           INTEGER PRIMARY KEY AUTOINCREMENT,
            title        TEXT    NOT NULL,
            link         TEXT    NOT NULL,
            description  TEXT,
            published_at TEXT,
            fetched_at   TEXT    NOT NULL
        )
        """);

    public bool IsStale()
    {
        var fetched = db.QueryOne(
            "SELECT fetched_at FROM rss_articles LIMIT 1",
            r => r.Field<string>("fetched_at"));
        if (fetched is null) return true;
        if (!DateTime.TryParse(fetched, out var dt)) return true;
        return (DateTime.UtcNow - dt).TotalMinutes > 30;
    }

    public IEnumerable<ArticleItem> GetArticles(int count) =>
        db.Query(
            "SELECT title, link, description, published_at FROM rss_articles ORDER BY published_at DESC LIMIT $count",
            Map,
            cmd => cmd.AddParam("$count", count));

    public void CacheArticles(IEnumerable<ArticleItem> articles)
    {
        var fetchedAt = DateTime.UtcNow.ToString("o");
        db.NonQuery("DELETE FROM rss_articles");
        foreach (var a in articles)
        {
            db.NonQuery("""
                INSERT INTO rss_articles (title, link, description, published_at, fetched_at)
                VALUES ($title, $link, $description, $published_at, $fetched_at)
                """, cmd =>
            {
                cmd.AddParam("$title", a.Title);
                cmd.AddParam("$link", a.Link);
                cmd.AddParam("$description", a.Description);
                cmd.AddParam("$published_at", a.PublishedAt);
                cmd.AddParam("$fetched_at", fetchedAt);
            });
        }
    }

    private static ArticleItem Map(DbDataReader r) =>
        new(r.Field<string>("title")!,
            r.Field<string>("link")!,
            r.Field<string?>("description"),
            r.Field<string?>("published_at"));
}
