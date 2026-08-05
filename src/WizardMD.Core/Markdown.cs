using WizardMD.Core.Ast;

namespace WizardMD.Core
{
    public static class Markdown
    {
        public static Document Parse(string text)
        {
            return new BlockParser(text).Parse();
        }

        public static string ToHtml(string text)
        {
            return HtmlRenderer.Render(Parse(text));
        }
    }
}