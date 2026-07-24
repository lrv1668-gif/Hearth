using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;
using Rss.Records;

namespace Rss;

public sealed class RssFetcher
{
    private static readonly XNamespace Atom = "http://www.w3.org/2005/Atom";

    /// <summary>
    /// Fetches and parses a feed, connecting to <paramref name="pinnedAddress"/> rather
    /// than re-resolving the URL's host. The address must come from
    /// <see cref="FeedUrlValidator.ResolvePinnedAddressAsync"/> for the same URL — using a
    /// pinned address closes the gap where the host's DNS answer could change between
    /// validation and fetch (DNS rebinding). TLS/SNI and the Host header still use the
    /// URL's original hostname, so HTTPS virtual hosting and certificate validation are
    /// unaffected. Redirects are not followed, since a redirect target hasn't been validated.
    /// </summary>
    public async Task<(string FeedTitle, IEnumerable<ArticleItem> Articles)?> FetchAsync(string url, IPAddress pinnedAddress)
    {
        try
        {
            using var handler = new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                ConnectCallback = async (context, ct) =>
                {
                    var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    await socket.ConnectAsync(pinnedAddress, context.DnsEndPoint.Port, ct);
                    return new NetworkStream(socket, ownsSocket: true);
                },
            };
            using var http = new HttpClient(handler);

            var content = await http.GetStringAsync(new Uri(url));
            return ParseFeed(url, content);
        }
        catch
        {
            return null;
        }
    }

    internal static (string FeedTitle, IEnumerable<ArticleItem> Articles) ParseFeed(string url, string xml)
    {
        var doc = XDocument.Parse(xml);

        // Atom feed
        var entries = doc.Descendants(Atom + "entry").ToList();
        if (entries.Count > 0)
        {
            var feedTitle = TitleSanitizer.ToPlainText(
                doc.Descendants(Atom + "title").FirstOrDefault()?.Value ?? url);
            var articles = entries.Select(e => new ArticleItem(
                TitleSanitizer.ToPlainText(e.Element(Atom + "title")?.Value),
                LinkSanitizer.ToSafeHref(e.Elements(Atom + "link")
                    .FirstOrDefault(l => l.Attribute("rel")?.Value != "self")
                    ?.Attribute("href")?.Value),
                e.Element(Atom + "summary")?.Value ?? e.Element(Atom + "content")?.Value,
                e.Element(Atom + "published")?.Value ?? e.Element(Atom + "updated")?.Value));
            return (feedTitle, articles);
        }

        // RSS 2.0 fallback
        var channelTitle = TitleSanitizer.ToPlainText(
            doc.Descendants("channel").FirstOrDefault()?.Element("title")?.Value ?? url);
        var rssArticles = doc.Descendants("item").Select(item => new ArticleItem(
            TitleSanitizer.ToPlainText(item.Element("title")?.Value),
            LinkSanitizer.ToSafeHref(item.Element("link")?.Value),
            item.Element("description")?.Value,
            item.Element("pubDate")?.Value));
        return (channelTitle, rssArticles);
    }
}
