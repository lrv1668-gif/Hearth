namespace Rss.Tests;

public sealed class LinkSanitizerTests
{
    [Theory]
    [InlineData("https://example.com/a")]
    [InlineData("http://example.com/a")]
    public void ToSafeHref_HttpOrHttpsUrl_PassesThroughUnchanged(string url)
    {
        Assert.Equal(url, LinkSanitizer.ToSafeHref(url));
    }

    [Theory]
    [InlineData("javascript:alert(document.cookie)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("/foo/bar")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToSafeHref_UnsafeRelativeOrBlank_ReturnsEmpty(string? url)
    {
        Assert.Equal("", LinkSanitizer.ToSafeHref(url));
    }
}
