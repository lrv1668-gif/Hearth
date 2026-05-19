using System.Xml.Linq;
using Rss.Records;

namespace Rss;

public sealed class RssFetcher(HttpClient http)
{
    private static readonly XNamespace Atom = "http://www.w3.org/2005/Atom";

    public async Task<(string FeedTitle, IEnumerable<ArticleItem> Articles)?> FetchAsync(string url)
    {
        try
        {
            var content = await http.GetStringAsync(url);
            var doc = XDocument.Parse(content);

            // Atom feed
            var entries = doc.Descendants(Atom + "entry").ToList();
            if (entries.Count > 0)
            {
                var feedTitle = doc.Descendants(Atom + "title").FirstOrDefault()?.Value ?? url;
                var articles = entries.Select(e => new ArticleItem(
                    e.Element(Atom + "title")?.Value ?? "",
                    e.Elements(Atom + "link")
                        .FirstOrDefault(l => l.Attribute("rel")?.Value != "self")
                        ?.Attribute("href")?.Value ?? "",
                    e.Element(Atom + "summary")?.Value ?? e.Element(Atom + "content")?.Value,
                    e.Element(Atom + "published")?.Value ?? e.Element(Atom + "updated")?.Value));
                return (feedTitle, articles);
            }

            // RSS 2.0 fallback
            var channelTitle = doc.Descendants("channel").FirstOrDefault()?.Element("title")?.Value ?? url;
            var rssArticles = doc.Descendants("item").Select(item => new ArticleItem(
                item.Element("title")?.Value ?? "",
                item.Element("link")?.Value ?? "",
                item.Element("description")?.Value,
                item.Element("pubDate")?.Value));
            return (channelTitle, rssArticles);
        }
        catch
        {
            return null;
        }
    }
}
