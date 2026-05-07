using System.Xml.Linq;
using Rss.Records;

namespace Rss;

public sealed class RssFetcher(HttpClient http)
{
    private const string FeedUrl = "https://www.theverge.com/rss/index.xml";
    private static readonly XNamespace Atom = "http://www.w3.org/2005/Atom";

    public async Task<IEnumerable<ArticleItem>> FetchAsync()
    {
        var content = await http.GetStringAsync(FeedUrl);
        var doc = XDocument.Parse(content);

        // Atom feed
        var entries = doc.Descendants(Atom + "entry").ToList();
        if (entries.Count > 0)
            return entries.Select(e => new ArticleItem(
                e.Element(Atom + "title")?.Value ?? "",
                e.Elements(Atom + "link")
                    .FirstOrDefault(l => l.Attribute("rel")?.Value != "self")
                    ?.Attribute("href")?.Value ?? "",
                e.Element(Atom + "summary")?.Value ?? e.Element(Atom + "content")?.Value,
                e.Element(Atom + "published")?.Value ?? e.Element(Atom + "updated")?.Value));

        // RSS 2.0 fallback
        return doc.Descendants("item").Select(item => new ArticleItem(
            item.Element("title")?.Value ?? "",
            item.Element("link")?.Value ?? "",
            item.Element("description")?.Value,
            item.Element("pubDate")?.Value));
    }
}
