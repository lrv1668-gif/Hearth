using Xunit;

namespace Rss.Tests;

public sealed class TitleSanitizerTests
{
    [Fact]
    public void ToPlainText_ScriptTag_StripsTags()
    {
        var result = TitleSanitizer.ToPlainText(
            """Breaking: <script src="https://evil.example/x.js"></script>Markets rally""");

        Assert.Equal("Breaking: Markets rally", result);
    }

    [Fact]
    public void ToPlainText_NestedTags_StripsAllTags()
    {
        var result = TitleSanitizer.ToPlainText("<div><b>Big</b> news <i>today</i></div>");

        Assert.Equal("Big news today", result);
    }

    [Fact]
    public void ToPlainText_MalformedUnclosedTag_StripsRemainder()
    {
        var result = TitleSanitizer.ToPlainText("Headline <img src=x onerror=alert(1)");

        Assert.Equal("Headline", result);
    }

    [Fact]
    public void ToPlainText_AttributeWithEventHandler_StripsTag()
    {
        var result = TitleSanitizer.ToPlainText("""Before <img src="x" onerror="alert(1)"> after""");

        Assert.Equal("Before  after", result);
    }

    [Fact]
    public void ToPlainText_HtmlEntities_Decoded()
    {
        var result = TitleSanitizer.ToPlainText("Fish &amp; Chips &#8212; a &quot;review&quot;");

        Assert.Equal("Fish & Chips — a \"review\"", result);
    }

    [Fact]
    public void ToPlainText_PlainTitle_PassesThroughUnchanged()
    {
        var result = TitleSanitizer.ToPlainText("Quiet morning headlines");

        Assert.Equal("Quiet morning headlines", result);
    }

    [Fact]
    public void ToPlainText_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Equal("", TitleSanitizer.ToPlainText(null));
        Assert.Equal("", TitleSanitizer.ToPlainText(""));
    }

    [Fact]
    public void ToPlainText_SurroundingWhitespace_Trimmed()
    {
        Assert.Equal("Spaced out", TitleSanitizer.ToPlainText("  Spaced out \n"));
    }
}
