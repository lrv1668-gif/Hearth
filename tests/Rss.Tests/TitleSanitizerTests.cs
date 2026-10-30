namespace Rss.Tests;

public sealed class TitleSanitizerTests
{
    [Theory]
    [InlineData("""Breaking: <script src="https://evil.example/x.js"></script>Markets rally""", "Breaking: Markets rally")]
    [InlineData("<div><b>Big</b> news <i>today</i></div>", "Big news today")]
    [InlineData("Headline <img src=x onerror=alert(1)", "Headline")] // unclosed tag
    [InlineData("""Before <img src="x" onerror="alert(1)"> after""", "Before  after")]
    [InlineData("Fish &amp; Chips &#8212; a &quot;review&quot;", "Fish & Chips — a \"review\"")]
    [InlineData("Quiet morning headlines", "Quiet morning headlines")]
    [InlineData("  Spaced out \n", "Spaced out")]
    [InlineData(null, "")]
    [InlineData("", "")]
    public void ToPlainText_Title_StripsTagsAndDecodesEntities(string? title, string expected)
    {
        Assert.Equal(expected, TitleSanitizer.ToPlainText(title));
    }
}
