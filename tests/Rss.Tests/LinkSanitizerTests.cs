using Xunit;

namespace Rss.Tests;

public sealed class LinkSanitizerTests
{
    [Fact]
    public void ToSafeHref_HttpsUrl_PassesThroughUnchanged()
    {
        Assert.Equal("https://example.com/a", LinkSanitizer.ToSafeHref("https://example.com/a"));
    }

    [Fact]
    public void ToSafeHref_HttpUrl_PassesThroughUnchanged()
    {
        Assert.Equal("http://example.com/a", LinkSanitizer.ToSafeHref("http://example.com/a"));
    }

    [Fact]
    public void ToSafeHref_JavascriptScheme_ReturnsEmpty()
    {
        Assert.Equal("", LinkSanitizer.ToSafeHref("javascript:alert(document.cookie)"));
    }

    [Fact]
    public void ToSafeHref_DataScheme_ReturnsEmpty()
    {
        Assert.Equal("", LinkSanitizer.ToSafeHref("data:text/html,<script>alert(1)</script>"));
    }

    [Fact]
    public void ToSafeHref_RelativePath_ReturnsEmpty()
    {
        Assert.Equal("", LinkSanitizer.ToSafeHref("/foo/bar"));
    }

    [Fact]
    public void ToSafeHref_NullEmptyOrWhitespace_ReturnsEmpty()
    {
        Assert.Equal("", LinkSanitizer.ToSafeHref(null));
        Assert.Equal("", LinkSanitizer.ToSafeHref(""));
        Assert.Equal("", LinkSanitizer.ToSafeHref("   "));
    }
}
