namespace WizardMD.Core.Tests;

public class RendererPageTests
{
    [Fact]
    public void Build_ProducesValidPage()
    {
        string md = "# Заголовок\n\n```csharp\nvar x = 1; // comment\n```\n";
        string page = WizardMD.App.MarkdownPage.Build(md, dark: false);
        Assert.Contains("<!DOCTYPE html>", page);
        Assert.Contains("<h1>Заголовок</h1>", page);
        Assert.Contains("language-csharp", page);
        Assert.Contains("color-scheme: light", page);
        Assert.Contains("</script>", page);
    }

    [Fact]
    public void Build_DarkThemeUsesDarkCss()
    {
        string page = WizardMD.App.MarkdownPage.Build("# T", dark: true);
        Assert.Contains("color-scheme: dark", page);
        Assert.DoesNotContain("color-scheme: light", page);
    }

    [Fact]
    public void Build_EmptyMarkdownProducesPage()
    {
        string page = WizardMD.App.MarkdownPage.Build("", dark: false);
        Assert.Contains("<main>", page);
        Assert.Contains("</main>", page);
    }
}