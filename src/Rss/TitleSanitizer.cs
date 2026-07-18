using System.Net;
using System.Text;

namespace Rss;

/// <summary>
/// Reduces feed and article titles to plain text so they are safe to store
/// and render anywhere. Strips HTML tags (including unclosed/malformed ones)
/// and decodes HTML entities (e.g. <c>&amp;amp;</c> → <c>&amp;</c>).
/// </summary>
public static class TitleSanitizer
{
    public static string ToPlainText(string? title)
    {
        if (string.IsNullOrEmpty(title)) return "";

        var sb = new StringBuilder(title.Length);
        var inTag = false;
        foreach (var c in title)
        {
            if (inTag)
            {
                if (c == '>') inTag = false;
            }
            else if (c == '<')
            {
                // An unclosed tag (no matching '>') drops the rest of the
                // string, which is the safe choice for malformed markup.
                inTag = true;
            }
            else
            {
                sb.Append(c);
            }
        }

        return WebUtility.HtmlDecode(sb.ToString()).Trim();
    }
}
