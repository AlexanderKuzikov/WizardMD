namespace WizardMD.Core.Tests;

public class GoldenTests
{
    [Fact]
    public void RendersRealMarkdownFiles_WithoutErrors()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "README.md"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "CONTEXT.md"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "DECISIONS.md"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "HANDOFF.md"),
        };

        int parsed = 0;
        foreach (var path in candidates.Where(File.Exists))
        {
            string md = File.ReadAllText(path);
            string html = WizardMD.Core.Markdown.ToHtml(md);
            Assert.False(string.IsNullOrEmpty(html));
            Assert.Contains("<", html);
            parsed++;
        }
        Assert.True(parsed > 0, "No real .md files found for golden test");
    }

    [Fact]
    public void RendersKnowledgeMarkdown()
    {
        string md = "# Заголовок\n\nПараграф с **жирным** и *курсивом* текстом, [ссылкой](/url) и `кодом`.\n\n- пункт 1\n- пункт 2\n\n1. первый\n2. второй\n\n> цитата\n\n```csharp\nint x = 1;\n```\n";
        string html = WizardMD.Core.Markdown.ToHtml(md);
        Assert.Contains("<h1>Заголовок</h1>", html);
        Assert.Contains("<strong>жирным</strong>", html);
        Assert.Contains("<a href=\"/url\">ссылкой</a>", html);
        Assert.Contains("<code>кодом</code>", html);
        Assert.Contains("<ul>", html);
        Assert.Contains("<ol>", html);
        Assert.Contains("<blockquote>", html);
        Assert.Contains("language-csharp", html);
    }
}