namespace WizardMD.Core.Tests;

public class DebugTests
{
    private static string Render(string md) => WizardMD.Core.Markdown.ToHtml(md);

    [Fact]
    public void Tab_Ex1()
    {
        Assert.Equal("<pre><code>foo\tbaz\t\tbim\n</code></pre>\n", Render("\tfoo\tbaz\t\tbim\n"));
    }

    [Fact]
    public void Tab_Ex5()
    {
        Assert.Equal("<ul>\n<li>\n<p>foo</p>\n<pre><code>  bar\n</code></pre>\n</li>\n</ul>\n", Render("- foo\n\n\t\tbar\n"));
    }

    [Fact]
    public void Tab_Ex6()
    {
        Assert.Equal("<blockquote>\n<pre><code>  foo\n</code></pre>\n</blockquote>\n", Render(">\t\tfoo\n"));
    }

    [Fact]
    public void Tab_Ex7()
    {
        Assert.Equal("<ul>\n<li>\n<pre><code>  foo\n</code></pre>\n</li>\n</ul>\n", Render("-\t\tfoo\n"));
    }

    [Theory]
    [InlineData("- a\n  - b", "<ul>\n<li>a\n<ul>\n<li>b</li>\n</ul>\n</li>\n</ul>\n")]
    [InlineData("- foo\n\n- bar", "<ul>\n<li>\n<p>foo</p>\n</li>\n<li>\n<p>bar</p>\n</li>\n</ul>\n")]
    [InlineData("- foo\n-\n- bar", "<ul>\n<li>foo</li>\n<li></li>\n<li>bar</li>\n</ul>\n")]
    [InlineData("The number of windows in my house is\n14.  The number of doors is 6.", "<p>The number of windows in my house is\n14.  The number of doors is 6.</p>\n")]
    [InlineData("-\n  foo", "<ul>\n<li>foo</li>\n</ul>\n")]
    public void Lists(string md, string expected)
    {
        Assert.Equal(expected, Render(md));
    }
}
public class AstDebug
{
[Fact]
    public void Dump()
    {
        var doc = WizardMD.Core.Markdown.Parse("- a\n  - b");
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(Markdown.ToHtml("- b").Replace("\n", "\\n"));
        sb.AppendLine(Markdown.ToHtml("- a\n  - b").Replace("\n", "\\n"));
        DumpNode(doc, sb, 0);
        System.IO.File.WriteAllText(@"D:\GitHub\WizardMD\tests\WizardMD.Core.Tests\bin\Debug\net8.0\ast.txt", sb.ToString());
    }

    private static void DumpNode(WizardMD.Core.Ast.Node n, System.Text.StringBuilder sb, int depth)
    {
        sb.Append(new string(' ', depth * 2)).Append(n.GetType().Name);
        if (n is WizardMD.Core.Ast.ParagraphBlock p) sb.Append(" raw=").Append(string.Join("|", p.RawLines));
        if (n is WizardMD.Core.Ast.CodeBlock c) sb.Append(" text=").Append(c.Text.Replace("\n", "\\n"));
        if (n is WizardMD.Core.Ast.ListBlock l) sb.Append(" ordered=").Append(l.IsOrdered).Append(" loose=").Append(l.IsLoose);
        sb.Append('\n');
        foreach (System.Reflection.PropertyInfo pi in n.GetType().GetProperties())
        {
            if (pi.PropertyType == typeof(System.Collections.Generic.List<WizardMD.Core.Ast.Node>) || pi.PropertyType == typeof(System.Collections.Generic.List<WizardMD.Core.Ast.ListItemBlock>))
            {
                var val = pi.GetValue(n) as System.Collections.IEnumerable;
                if (val == null) continue;
                foreach (var child in val)
                    if (child is WizardMD.Core.Ast.Node cn) DumpNode(cn, sb, depth + 1);
            }
        }
    }
}
