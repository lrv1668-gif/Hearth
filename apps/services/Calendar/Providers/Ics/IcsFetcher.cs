using System.Net;
using ServiceDefaults;

namespace Calendar.Providers.Ics;

/// <summary>A problem with a subscribed calendar that the household can act on; the message is shown in Settings.</summary>
public sealed class IcsFeedException(string message) : Exception(message);

/// <summary>Reads the iCalendar text behind a subscription link.</summary>
public interface IIcsSource
{
    /// <summary>
    /// The feed's text. Throws <see cref="IcsFeedException"/> when the link isn't allowed, and
    /// whatever the network throws when it can't be read.
    /// </summary>
    Task<string> FetchAsync(string url, CancellationToken ct = default);
}

/// <summary>
/// Fetches a subscription link the way Rss fetches a feed: only public addresses, connected
/// to the validated address, no redirects — a pasted link must not reach the LAN.
/// </summary>
public sealed class IcsFetcher(FeedUrlValidator validator) : IIcsSource
{
    private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(15);

    // A decade of a busy family calendar is a few MB; past this it isn't a calendar
    internal const int MaxBytes = 10 * 1024 * 1024;

    /// <summary>
    /// The link as fetched: trimmed, with Apple's <c>webcal://</c> (and <c>webcals://</c>) read
    /// as <c>https://</c>. Null unless the result is an absolute http(s) URL.
    /// </summary>
    public static string? NormalizeUrl(string? url)
    {
        var trimmed = url?.Trim() ?? "";
        foreach (var scheme in (string[])["webcal://", "webcals://"])
        {
            if (trimmed.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
                trimmed = "https://" + trimmed[scheme.Length..];
        }

        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri.AbsoluteUri
            : null;
    }

    public async Task<string> FetchAsync(string url, CancellationToken ct = default)
    {
        var pinned = await validator.ResolvePinnedAddressAsync(url)
            ?? throw new IcsFeedException("That link can't be used: it must be a public http(s) address.");
        return await FetchPinnedAsync(url, pinned, ct);
    }

    internal static async Task<string> FetchPinnedAsync(string url, IPAddress pinnedAddress, CancellationToken ct)
    {
        using var handler = PinnedHttpHandler.Create(pinnedAddress);
        using var http = new HttpClient(handler) { Timeout = FetchTimeout, MaxResponseContentBufferSize = MaxBytes };
        return await http.GetStringAsync(new Uri(url), ct);
    }
}
