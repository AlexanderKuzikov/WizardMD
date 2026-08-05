using System.Collections.Generic;
using System.Text;
using WizardMD.Core.Ast;

namespace WizardMD.Core
{
    public static class HtmlRenderer
    {
        public static string Render(Document doc)
        {
            var sb = new StringBuilder();
            foreach (var node in doc.Blocks) RenderBlock(node, sb);
            return sb.ToString();
        }

        private static void RenderBlock(Node node, StringBuilder sb)
        {
            if (node is ParagraphBlock p)
            {
                sb.Append("<p>");
                RenderInlines(p.Inlines, sb);
                sb.Append("</p>\n");
            }
            else if (node is HeadingBlock h)
            {
                sb.Append("<h").Append(h.Level).Append('>');
                RenderInlines(h.Inlines, sb);
                sb.Append("</h").Append(h.Level).Append(">\n");
            }
            else if (node is ListBlock list)
            {
                if (list.IsOrdered)
                {
                    sb.Append("<ol");
                    if (list.Start != 1) sb.Append(" start=\"").Append(list.Start).Append('"');
                    sb.Append(">\n");
                }
                else
                {
                    sb.Append("<ul>\n");
                }
                foreach (var item in list.Items) RenderListItem(item, sb);
                sb.Append(list.IsOrdered ? "</ol>\n" : "</ul>\n");
            }
            else if (node is BlockQuoteBlock q)
            {
                sb.Append("<blockquote>\n");
                foreach (var inner in q.Blocks) RenderBlock(inner, sb);
                sb.Append("</blockquote>\n");
            }
            else if (node is CodeBlock code)
            {
                sb.Append("<pre><code");
                if (!string.IsNullOrEmpty(code.Info))
                {
                    string lang = code.Info.Split(' ', '\t')[0];
                    sb.Append(" class=\"language-").Append(EscapeAttr(lang)).Append('"');
                }
                sb.Append('>');
                sb.Append(Escape(code.Text));
                sb.Append("</code></pre>\n");
            }
            else if (node is ThematicBreakBlock)
            {
                sb.Append("<hr />\n");
            }
            else if (node is TableBlock table)
            {
                RenderTable(table, sb);
            }
        }

        private static void RenderListItem(ListItemBlock item, StringBuilder sb)
        {
            sb.Append("<li");
            if (item.IsTask)
            {
                sb.Append(" class=\"task-list-item\"");
                sb.Append("><input type=\"checkbox\" disabled=\"\"");
                if (item.TaskChecked) sb.Append(" checked=\"\"");
                sb.Append("> ");
            }
            else
            {
                sb.Append('>');
            }
            foreach (var inner in item.Blocks)
            {
                if (inner is ParagraphBlock para)
                {
                    RenderInlines(para.Inlines, sb);
                    AppendIfNeeded(sb, '\n');
                }
                else
                {
                    RenderBlock(inner, sb);
                }
            }
            sb.Append("</li>\n");
        }

        private static void AppendIfNeeded(StringBuilder sb, char c)
        {
            if (sb.Length > 0 && sb[sb.Length - 1] != c) sb.Append(c);
        }

        private static void RenderTable(TableBlock table, StringBuilder sb)
        {
            sb.Append("<table>\n");
            if (table.HasHeader && table.Rows.Count > 0)
            {
                sb.Append("<thead>\n<tr>\n");
                RenderTableRow(table.Rows[0], table.Aligns, true, sb);
                sb.Append("</tr>\n</thead>\n");
            }
            int start = table.HasHeader ? 1 : 0;
            if (start < table.Rows.Count)
            {
                sb.Append("<tbody>\n");
                for (int r = start; r < table.Rows.Count; r++)
                {
                    sb.Append("<tr>\n");
                    RenderTableRow(table.Rows[r], table.Aligns, false, sb);
                    sb.Append("</tr>\n");
                }
                sb.Append("</tbody>\n");
            }
            sb.Append("</table>\n");
        }

        private static void RenderTableRow(TableRow row, List<TableAlign> aligns, bool isHeader, StringBuilder sb)
        {
            for (int i = 0; i < row.Cells.Count; i++)
            {
                string tag = isHeader ? "th" : "td";
                sb.Append('<').Append(tag);
                if (i < aligns.Count && aligns[i] == TableAlign.Left) sb.Append(" align=\"left\"");
                else if (i < aligns.Count && aligns[i] == TableAlign.Center) sb.Append(" align=\"center\"");
                else if (i < aligns.Count && aligns[i] == TableAlign.Right) sb.Append(" align=\"right\"");
                sb.Append('>');
                RenderInlines(row.Cells[i], sb);
                sb.Append("</").Append(tag).Append(">\n");
            }
        }

        private static void RenderInlines(List<InlineNode> inlines, StringBuilder sb)
        {
            foreach (var node in inlines)
            {
                if (node is TextNode t)
                {
                    sb.Append(Escape(t.Text));
                }
                else if (node is StrongNode strong)
                {
                    sb.Append("<strong>");
                    RenderInlines(strong.Children, sb);
                    sb.Append("</strong>");
                }
                else if (node is EmphasisNode em)
                {
                    sb.Append("<em>");
                    RenderInlines(em.Children, sb);
                    sb.Append("</em>");
                }
                else if (node is StrikethroughNode del)
                {
                    sb.Append("<del>");
                    RenderInlines(del.Children, sb);
                    sb.Append("</del>");
                }
                else if (node is CodeNode code)
                {
                    sb.Append("<code>").Append(Escape(code.Text)).Append("</code>");
                }
                else if (node is LinkNode link)
                {
                    sb.Append("<a href=\"").Append(EscapeAttr(link.Url)).Append('"');
                    if (link.Title.Length > 0) sb.Append(" title=\"").Append(EscapeAttr(link.Title)).Append('"');
                    sb.Append('>');
                    RenderInlines(link.Children, sb);
                    sb.Append("</a>");
                }
                else if (node is ImageNode img)
                {
                    sb.Append("<img src=\"").Append(EscapeAttr(img.Url)).Append("\" alt=\"").Append(EscapeAttr(PlainAlt(img.Children))).Append('"');
                    if (img.Title.Length > 0) sb.Append(" title=\"").Append(EscapeAttr(img.Title)).Append('"');
                    sb.Append(" />");
                }
                else if (node is AutoLinkNode auto)
                {
                    sb.Append("<a href=\"").Append(EscapeAttr(auto.Url)).Append("\">").Append(Escape(auto.Label)).Append("</a>");
                }
                else if (node is SoftBreakNode)
                {
                    sb.Append('\n');
                }
                else if (node is HardBreakNode)
                {
                    sb.Append("<br />\n");
                }
            }
        }

        private static string PlainAlt(List<InlineNode> children)
        {
            var sb = new StringBuilder();
            foreach (var node in children)
            {
                if (node is TextNode t) sb.Append(t.Text);
                else if (node is CodeNode c) sb.Append(c.Text);
                else if (node is StrongNode s) sb.Append(PlainAlt(s.Children));
                else if (node is EmphasisNode e) sb.Append(PlainAlt(e.Children));
                else if (node is StrikethroughNode d) sb.Append(PlainAlt(d.Children));
                else if (node is ImageNode i) sb.Append(PlainAlt(i.Children));
            }
            return sb.ToString();
        }

        public static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length + 8);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '&': sb.Append("&amp;"); break;
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '"': sb.Append("&quot;"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        private static string EscapeAttr(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length + 8);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '&': sb.Append("&amp;"); break;
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '"': sb.Append("&quot;"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }
    }
}