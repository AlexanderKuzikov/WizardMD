namespace WizardMD.Core.Tests;

public class SpecTests
{
    public class Example
    {
        public string markdown { get; set; } = "";
        public string html { get; set; } = "";
        public int example { get; set; }
        public string section { get; set; } = "";
    }

    [Fact]
    public void Spec_Run_CommonMark030()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "spec.json");
        Assert.True(File.Exists(path), "spec.json not found");
        var json = File.ReadAllText(path);
        var examples = System.Text.Json.JsonSerializer.Deserialize<List<Example>>(json)
                       ?? new List<Example>();
        Assert.Equal(652, examples.Count);

        int passed = 0;
        var failed = new List<(int example, string section, string md, string expected, string actual)>();
        foreach (var ex in examples)
        {
            string actual = WizardMD.Core.Markdown.ToHtml(ex.markdown);
            if (actual == ex.html) passed++;
            else failed.Add((ex.example, ex.section, ex.markdown, ex.html, actual));
        }

        double pct = 100.0 * passed / examples.Count;
        var bySection = failed
            .GroupBy(f => f.section)
            .Select(g => $"{g.Key,-40} {examples.Count(e => e.section == g.Key) - g.Count(),3}/{examples.Count(e => e.section == g.Key),3}");
        var report = $"CommonMark spec: {passed}/{examples.Count} ({pct:F1}%)\n\n" +
            "Passed by section:\n" + string.Join("\n", bySection) + "\n\n" +
            string.Join("\n", failed.Take(40).Select(f =>
                $"#{f.example} [{f.section}]\n  md : {Escape(f.md)}\n  exp: {Escape(f.expected)}\n  act: {Escape(f.actual)}"));
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "spec-report.txt"), report);
        Assert.True(pct >= 80.0, $"CommonMark spec: {passed}/{examples.Count} ({pct:F1}%) — below 80%.");
    }

    private static string Escape(string s) => s.Replace("\n", "\\n\n      ");
}