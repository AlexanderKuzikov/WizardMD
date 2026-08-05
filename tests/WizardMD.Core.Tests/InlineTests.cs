namespace WizardMD.Core.Tests;

public class InlineTests
{
    private static string Render(string md) => WizardMD.Core.Markdown.ToHtml(md);

    [Theory]
    [InlineData("*foo*", "<p><em>foo</em></p>\n")]
    [InlineData("**foo**", "<p><strong>foo</strong></p>\n")]
    [InlineData("***foo***", "<p><em><strong>foo</strong></em></p>\n")]
    [InlineData("foo *bar* baz", "<p>foo <em>bar</em> baz</p>\n")]
    [InlineData("*foo **bar** baz*", "<p><em>foo <strong>bar</strong> baz</em></p>\n")]
    [InlineData("**foo *bar* baz**", "<p><strong>foo <em>bar</em> baz</strong></p>\n")]
    [InlineData("*foo* *bar*", "<p><em>foo</em> <em>bar</em></p>\n")]
    [InlineData("~~foo~~", "<p><del>foo</del></p>\n")]
    [InlineData("foo_bar_baz", "<p>foo_bar_baz</p>\n")]
    [InlineData("a*\"foo\"*", "<p>a*&quot;foo&quot;*</p>\n")]
    public void Emphasis(string md, string expected)
    {
        Assert.Equal(expected, Render(md));
    }

    [Theory]
    [InlineData("`code`", "<p><code>code</code></p>\n")]
    [InlineData("``code ` span``", "<p><code>code ` span</code></p>\n")]
    [InlineData("`code\nspan`", "<p><code>code span</code></p>\n")]
    [InlineData("a ` b ` c", "<p>a <code>b</code> c</p>\n")]
    public void CodeSpans(string md, string expected)
    {
        Assert.Equal(expected, Render(md));
    }

    [Theory]
    [InlineData("[text](/url)", "<p><a href=\"/url\">text</a></p>\n")]
    [InlineData("[text](/url \"title\")", "<p><a href=\"/url\" title=\"title\">text</a></p>\n")]
    [InlineData("[**bold** link](/url)", "<p><a href=\"/url\"><strong>bold</strong> link</a></p>\n")]
    [InlineData("[link](<my url>)", "<p><a href=\"my%20url\">link</a></p>\n")]
    [InlineData("[foo][bar]\n\n[bar]: /url\n", "<p><a href=\"/url\">foo</a></p>\n")]
    [InlineData("[foo]\n\n[foo]: /url \"t\"\n", "<p><a href=\"/url\" title=\"t\">foo</a></p>\n")]
    [InlineData("[foo](not a link)\n\n[foo]: /url1\n", "<p><a href=\"/url1\">foo</a>(not a link)</p>\n")]
    [InlineData("[foo][bar]\n\n[foo]: /url1\n[bar]: /url2\n", "<p><a href=\"/url2\">foo</a></p>\n")]
    public void Links(string md, string expected)
    {
        Assert.Equal(expected, Render(md));
    }

    [Theory]
    [InlineData("![alt](/img.png)", "<p><img src=\"/img.png\" alt=\"alt\" /></p>\n")]
    [InlineData("![alt](/img.png \"t\")", "<p><img src=\"/img.png\" alt=\"alt\" title=\"t\" /></p>\n")]
    [InlineData("[![moon](moon.jpg)](/uri)", "<p><a href=\"/uri\"><img src=\"moon.jpg\" alt=\"moon\" /></a></p>\n")]
    [InlineData("![foo [bar](/url)](/url2)", "<p><img src=\"/url2\" alt=\"foo bar\" /></p>\n")]
    public void Images(string md, string expected)
    {
        Assert.Equal(expected, Render(md));
    }

    [Theory]
    [InlineData("<http://example.com>", "<p><a href=\"http://example.com\">http://example.com</a></p>\n")]
    [InlineData("<foo@bar.example.com>", "<p><a href=\"mailto:foo@bar.example.com\">foo@bar.example.com</a></p>\n")]
    [InlineData("<http://foo.bar/baz bim>", "<p>&lt;http://foo.bar/baz bim&gt;</p>\n")]
    [InlineData("<m:abc>", "<p>&lt;m:abc&gt;</p>\n")]
    public void Autolinks(string md, string expected)
    {
        Assert.Equal(expected, Render(md));
    }

    [Theory]
    [InlineData("\\*not em\\*", "<p>*not em*</p>\n")]
    [InlineData("\\# not heading", "<p># not heading</p>\n")]
    public void Escapes(string md, string expected)
    {
        Assert.Equal(expected, Render(md));
    }

    [Theory]
    [InlineData("&amp;", "<p>&amp;</p>\n")]
    [InlineData("&lt; &gt; &quot;", "<p>&lt; &gt; &quot;</p>\n")]
    [InlineData("&#35;", "<p>#</p>\n")]
    [InlineData("&notarealentity;", "<p>&amp;notarealentity;</p>\n")]
    public void Entities(string md, string expected)
    {
        Assert.Equal(expected, Render(md));
    }

    [Theory]
    [InlineData("foo  \nbar", "<p>foo<br />\nbar</p>\n")]
    [InlineData("foo\\\nbar", "<p>foo<br />\nbar</p>\n")]
    [InlineData("foo\nbar", "<p>foo\nbar</p>\n")]
    public void LineBreaks(string md, string expected)
    {
        Assert.Equal(expected, Render(md));
    }
}