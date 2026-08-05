namespace WizardMD.Core.Tests;

public class BlockTests
{
    private static string Render(string md) => WizardMD.Core.Markdown.ToHtml(md);

    [Theory]
    [InlineData("# Heading", "<h1>Heading</h1>\n")]
    [InlineData("## Sub *heading*", "<h2>Sub <em>heading</em></h2>\n")]
    [InlineData("### foo ###", "<h3>foo</h3>\n")]
    [InlineData("###### six", "<h6>six</h6>\n")]
    [InlineData("#7 no heading", "<p>#7 no heading</p>\n")]
    public void AtxHeadings(string md, string expected)
    {
        Assert.Equal(expected, Render(md));
    }

    [Theory]
    [InlineData("Foo *bar*\n=========\n", "<h1>Foo <em>bar</em></h1>\n")]
    [InlineData("Foo\n---\n", "<h2>Foo</h2>\n")]
    [InlineData("Foo\n\nBar\n===\n", "<p>Foo</p>\n<h1>Bar</h1>\n")]
    public void SetextHeadings(string md, string expected)
    {
        Assert.Equal(expected, Render(md));
    }

    [Theory]
    [InlineData("para one\npara two\n", "<p>para one\npara two</p>\n")]
    [InlineData("a\n\nb\n", "<p>a</p>\n<p>b</p>\n")]
    [InlineData("  leading spaces\n", "<p>leading spaces</p>\n")]
    public void Paragraphs(string md, string expected)
    {
        Assert.Equal(expected, Render(md));
    }

    [Theory]
    [InlineData("---\n", "<hr />\n")]
    [InlineData("***\n", "<hr />\n")]
    [InlineData("___\n", "<hr />\n")]
    [InlineData("- - -\n", "<hr />\n")]
    [InlineData("foo\n---\n", "<h2>foo</h2>\n")]
    public void ThematicBreaks(string md, string expected)
    {
        Assert.Equal(expected, Render(md));
    }

    [Theory]
    [InlineData("```\ncode\n```\n", "<pre><code>code\n</code></pre>\n")]
    [InlineData("```python\nprint(1)\n```\n", "<pre><code class=\"language-python\">print(1)\n</code></pre>\n")]
    [InlineData("~~~\ncode\n~~~\n", "<pre><code>code\n</code></pre>\n")]
    [InlineData("    indented\n", "<pre><code>indented\n</code></pre>\n")]
    [InlineData("```\nunclosed\n", "<pre><code>unclosed\n</code></pre>\n")]
    public void CodeBlocks(string md, string expected)
    {
        Assert.Equal(expected, Render(md));
    }

    [Theory]
    [InlineData("> quote\n", "<blockquote>\n<p>quote</p>\n</blockquote>\n")]
    [InlineData("> a\n> b\n", "<blockquote>\n<p>a\nb</p>\n</blockquote>\n")]
    [InlineData("> # h\n", "<blockquote>\n<h1>h</h1>\n</blockquote>\n")]
    [InlineData("> > nested\n", "<blockquote>\n<blockquote>\n<p>nested</p>\n</blockquote>\n</blockquote>\n")]
    [InlineData("> a\nlazy\n", "<blockquote>\n<p>a\nlazy</p>\n</blockquote>\n")]
    public void BlockQuotes(string md, string expected)
    {
        Assert.Equal(expected, Render(md));
    }

    [Theory]
    [InlineData("- a\n- b\n", "<ul>\n<li>a</li>\n<li>b</li>\n</ul>\n")]
    [InlineData("* a\n* b\n", "<ul>\n<li>a</li>\n<li>b</li>\n</ul>\n")]
    [InlineData("1. a\n2. b\n", "<ol>\n<li>a</li>\n<li>b</li>\n</ol>\n")]
    [InlineData("3. a\n4. b\n", "<ol start=\"3\">\n<li>a</li>\n<li>b</li>\n</ol>\n")]
    [InlineData("- a\n  - b\n", "<ul>\n<li>a\n<ul>\n<li>b</li>\n</ul>\n</li>\n</ul>\n")]
    [InlineData("- a\n\n- b\n", "<ul>\n<li>\n<p>a</p>\n</li>\n<li>\n<p>b</p>\n</li>\n</ul>\n")]
    [InlineData("- foo\n-\n- bar\n", "<ul>\n<li>foo</li>\n<li></li>\n<li>bar</li>\n</ul>\n")]
    [InlineData("- [x] done\n- [ ] todo\n", "<ul>\n<li class=\"task-list-item\"><input type=\"checkbox\" disabled=\"\" checked=\"\"> done</li>\n<li class=\"task-list-item\"><input type=\"checkbox\" disabled=\"\"> todo</li>\n</ul>\n")]
    [InlineData("The number of windows in my house is\n14.  The number of doors is 6.\n", "<p>The number of windows in my house is\n14.  The number of doors is 6.</p>\n")]
    public void Lists(string md, string expected)
    {
        Assert.Equal(expected, Render(md));
    }

    [Theory]
    [InlineData("| a | b |\n|---|---|\n| 1 | 2 |\n", "<table>\n<thead>\n<tr>\n<th>a</th>\n<th>b</th>\n</tr>\n</thead>\n<tbody>\n<tr>\n<td>1</td>\n<td>2</td>\n</tr>\n</tbody>\n</table>\n")]
    [InlineData("| a | b |\n|:--|--:|\n| 1 | 2 |\n", "<table>\n<thead>\n<tr>\n<th align=\"left\">a</th>\n<th align=\"right\">b</th>\n</tr>\n</thead>\n<tbody>\n<tr>\n<td align=\"left\">1</td>\n<td align=\"right\">2</td>\n</tr>\n</tbody>\n</table>\n")]
    public void Tables(string md, string expected)
    {
        Assert.Equal(expected, Render(md));
    }

    [Theory]
    [InlineData("\tfoo\n", "<pre><code>foo\n</code></pre>\n")]
    [InlineData("-\t\tfoo\n", "<ul>\n<li>\n<pre><code>  foo\n</code></pre>\n</li>\n</ul>\n")]
    [InlineData(">\t\tfoo\n", "<blockquote>\n<pre><code>  foo\n</code></pre>\n</blockquote>\n")]
    [InlineData("-\n  foo\n", "<ul>\n<li>foo</li>\n</ul>\n")]
    public void Tabs(string md, string expected)
    {
        Assert.Equal(expected, Render(md));
    }

    [Fact]
    public void DocWithAllBlocks()
    {
        string md = "# Title\n\nSome *text* with [link](/url).\n\n- item1\n- item2\n\n> quote\n\n```cs\nvar x = 1;\n```\n\n---\n\n| h1 | h2 |\n|---|---|\n| 1 | 2 |\n";
        string expected = "<h1>Title</h1>\n<p>Some <em>text</em> with <a href=\"/url\">link</a>.</p>\n<ul>\n<li>item1</li>\n<li>item2</li>\n</ul>\n<blockquote>\n<p>quote</p>\n</blockquote>\n<pre><code class=\"language-cs\">var x = 1;\n</code></pre>\n<hr />\n<table>\n<thead>\n<tr>\n<th>h1</th>\n<th>h2</th>\n</tr>\n</thead>\n<tbody>\n<tr>\n<td>1</td>\n<td>2</td>\n</tr>\n</tbody>\n</table>\n";
        Assert.Equal(expected, Render(md));
    }
}