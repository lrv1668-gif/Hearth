using System.Data.Common;
using System.Globalization;
using Data.Abstractions;
using Rss.Records;

namespace Rss;

public sealed class RssStore([FromKeyedServices("rss")] IDatabase db)
{
    public void Migrate()
    {
        db.NonQuery("""
            CREATE TABLE IF NOT EXISTS rss_articles (
                id           INTEGER PRIMARY KEY AUTOINCREMENT,
                feed_url     TEXT    NOT NULL DEFAULT '',
                feed_title   TEXT    NOT NULL DEFAULT '',
                title        TEXT    NOT NULL,
                link         TEXT    NOT NULL,
                description  TEXT,
                published_at TEXT,
                fetched_at   TEXT    NOT NULL
            )
            """);

        foreach (var col in new[]
        {
            "ALTER TABLE rss_articles ADD COLUMN feed_url TEXT NOT NULL DEFAULT ''",
            "ALTER TABLE rss_articles ADD COLUMN feed_title TEXT NOT NULL DEFAULT ''",
        })
        {
            try { db.NonQuery(col); } catch { /* column already exists */ }
        }
    }

    public bool IsStale(string feedUrl)
    {
        var fetched = db.QueryOne(
            "SELECT fetched_at FROM rss_articles WHERE feed_url = $url LIMIT 1",
            r => r.Field<string>("fetched_at"),
            cmd => cmd.AddParam("$url", feedUrl));
        if (fetched is null) return true;
        // RoundtripKind preserves Kind=Utc on the stored "...Z" timestamp; default
        // TryParse would convert to local time and skew the comparison against UtcNow.
        if (!DateTime.TryParse(fetched, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var dt)) return true;
        return (DateTime.UtcNow - dt).TotalMinutes > 30;
    }

    // Titles are sanitized again on the read path because rows cached before
    // sanitization existed (or by an older version) may still hold raw HTML.
    public string? GetFeedTitle(string feedUrl) =>
        db.QueryOne(
            "SELECT feed_title FROM rss_articles WHERE feed_url = $url LIMIT 1",
            r => TitleSanitizer.ToPlainText(r.Field<string>("feed_title")),
            cmd => cmd.AddParam("$url", feedUrl));

    public IEnumerable<ArticleItem> GetArticles(string feedUrl, int count) =>
        db.Query(
            "SELECT title, link, description, published_at FROM rss_articles WHERE feed_url = $url ORDER BY published_at DESC LIMIT $count",
            Map,
            cmd => { cmd.AddParam("$url", feedUrl); cmd.AddParam("$count", count); });

    public void CacheArticles(string feedUrl, string feedTitle, IEnumerable<ArticleItem> articles)
    {
        var fetchedAt = DateTime.UtcNow.ToString("o");
        db.NonQuery("DELETE FROM rss_articles WHERE feed_url = $url",
            cmd => cmd.AddParam("$url", feedUrl));
        foreach (var a in articles)
        {
            db.NonQuery("""
                INSERT INTO rss_articles (feed_url, feed_title, title, link, description, published_at, fetched_at)
                VALUES ($feed_url, $feed_title, $title, $link, $description, $published_at, $fetched_at)
                """, cmd =>
            {
                cmd.AddParam("$feed_url", feedUrl);
                cmd.AddParam("$feed_title", feedTitle);
                cmd.AddParam("$title", a.Title);
                cmd.AddParam("$link", a.Link);
                cmd.AddParam("$description", a.Description);
                cmd.AddParam("$published_at", a.PublishedAt);
                cmd.AddParam("$fetched_at", fetchedAt);
            });
        }
    }

    private static ArticleItem Map(DbDataReader r) =>
        new(TitleSanitizer.ToPlainText(r.Field<string>("title")),
            r.Field<string>("link")!,
            r.Field<string?>("description"),
            r.Field<string?>("published_at"));
}
