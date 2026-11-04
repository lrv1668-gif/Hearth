using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Rss.Records;
using ServiceDefaults;
using ZiggyCreatures.Caching.Fusion;

namespace Rss;

/// <summary>
/// The e-paper frame's Headlines: the newest stories across the household's feeds, picked
/// once per clock hour. The frame re-renders every 15 minutes and every change costs a
/// ~30 s panel redraw, so the list holds still until the next hour even as feeds update.
/// </summary>
public sealed class Headlines(RssFeeds feeds, IFusionCache cache, TimeProvider clock)
{
    public const int MaxCount = 20;

    /// <summary>The hour's headlines for <paramref name="feedUrls"/> (already validated and pinned).</summary>
    public async Task<IReadOnlyList<Headline>> GetAsync(
        IReadOnlyList<(string Url, System.Net.IPAddress Pinned)> feedUrls, int count, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow();
        var hour = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero);
        var options = new FusionCacheEntryOptions
        {
            // Until the top of the next hour, when a new edition is picked
            Duration = hour.AddHours(1) - now,
            FactorySoftTimeout = HearthCache.SoftTimeout,
        };

        return await cache.GetOrSetAsync<IReadOnlyList<Headline>>(
            $"headlines:{Key(feedUrls.Select(f => f.Url), count)}:{hour:yyyyMMddHH}",
            async (_, token) =>
            {
                var fetched = await Task.WhenAll(feedUrls.Select(f => feeds.GetAsync(f.Url, f.Pinned, token)));
                return Pick(fetched.OfType<Feed>(), count);
            },
            options, ct);
    }

    /// <summary>
    /// The newest stories, taken from each feed in turn (the freshest feed first) so one busy
    /// feed can't fill the column. Undated items sort after dated ones, in feed order; a title
    /// already picked (from another feed, or repeated) is skipped.
    /// </summary>
    public static IReadOnlyList<Headline> Pick(IEnumerable<Feed> feeds, int count)
    {
        var queues = feeds
            .Select(feed => feed.Articles
                .Where(a => !string.IsNullOrWhiteSpace(a.Title))
                .Select((a, i) => (Article: a, Order: i, Published: ParseDate(a.PublishedAt) ?? DateTimeOffset.MinValue))
                .OrderByDescending(a => a.Published)
                .ThenBy(a => a.Order)
                .Select(a => (a.Published, Headline: new Headline(a.Article.Title, feed.FeedTitle,
                    a.Published == DateTimeOffset.MinValue ? null : a.Published.ToString("o", CultureInfo.InvariantCulture))))
                .ToList())
            .Where(q => q.Count > 0)
            .OrderByDescending(q => q[0].Published)
            .Select(q => q.Select(a => a.Headline).ToList())
            .ToList();

        // Wire stories often run under the same title on several feeds: keep the first.
        var picked = new List<Headline>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var round = 0; picked.Count < count && queues.Any(q => q.Count > round); round++)
        {
            foreach (var queue in queues)
            {
                if (picked.Count == count) break;
                if (round < queue.Count && seen.Add(queue[round].Title.Trim())) picked.Add(queue[round]);
            }
        }
        return picked;
    }

    /// <summary>RSS dates are RFC 822 ("Tue, 04 Nov 2026 09:00:00 GMT"), Atom's ISO 8601.</summary>
    internal static DateTimeOffset? ParseDate(string? value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)
            ? date.ToUniversalTime()
            : null;

    private static string Key(IEnumerable<string> urls, int count) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{count}\n{string.Join('\n', urls)}")))[..16];
}
