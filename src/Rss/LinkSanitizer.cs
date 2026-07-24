namespace Rss;

/// <summary>
/// Restricts feed-supplied links to absolute http/https URLs before they are stored
/// or rendered, so a malicious feed cannot smuggle a javascript:/data: URI into an
/// &lt;a href&gt;.
/// </summary>
public static class LinkSanitizer
{
    public static string ToSafeHref(string? link)
    {
        if (string.IsNullOrWhiteSpace(link)) return "";
        if (!Uri.TryCreate(link, UriKind.Absolute, out var uri)) return "";
        return uri.Scheme is "http" or "https" ? link : "";
    }
}
