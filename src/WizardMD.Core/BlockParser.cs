using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WizardMD.Core.Ast;

namespace WizardMD.Core
{
    internal sealed class LinkReference
    {
        public string Label;
        public string Url;
        public string Title;

        public LinkReference(string label, string url, string title)
        {
            Label = label;
            Url = url;
            Title = title;
        }
    }

    internal struct ListMarkerInfo
    {
        public bool IsOrdered;
        public char BulletChar;
        public char Delimiter;
        public int Start;
        public int ContentIndent;
        public string Content;

        public ListMarkerInfo(bool ordered, char bullet, char delimiter, int start, int contentIndent, string content)
        {
            IsOrdered = ordered;
            BulletChar = bullet;
            Delimiter = delimiter;
            Start = start;
            ContentIndent = contentIndent;
            Content = content;
        }

        public bool CompatibleWith(ListMarkerInfo other)
        {
            if (IsOrdered != other.IsOrdered) return false;
            if (IsOrdered) return Delimiter == other.Delimiter;
            return BulletChar == other.BulletChar;
        }
    }

    internal sealed class BlockParser
    {
        private readonly string[] _lines;
        private int _pos;
        private readonly Dictionary<string, LinkReference> _references;

        public BlockParser(string text)
        {
            _lines = Normalize(text);
            _references = new Dictionary<string, LinkReference>();
        }

        private BlockParser(string[] lines, Dictionary<string, LinkReference> references)
        {
            _lines = lines;
            _references = references;
        }

        public static string[] Normalize(string text)
        {
            if (text == null) return new string[0];
            text = text.Replace("\r\n", "\n").Replace('\r', '\n');
            return text.Split('\n');
        }

        public Document Parse()
        {
            PreScanReferences();
            var doc = new Document();
            doc.Blocks.AddRange(ParseBlockList());
            return doc;
        }

        private void PreScanReferences()
        {
            for (int i = 0; i < _lines.Length; i++)
            {
                if (TryReferenceDefinition(_lines[i], out LinkReference r))
                    _references[r.Label] = r;
            }
        }

        public List<Node> ParseBlockList()
        {
            var container = new List<Node>();
            while (_pos < _lines.Length)
            {
                string line = _lines[_pos];
                if (IsBlank(line))
                {
                    _pos++;
                    continue;
                }
                if (TryProcessThematicBreak(container)) continue;
                if (TryProcessBlockquote(container)) continue;
                if (TryProcessList(container)) continue;
                if (TryProcessAtx(container)) continue;
                if (TryProcessFence(container)) continue;
                if (TryProcessIndentedCode(container)) continue;
                if (TryProcessReference(container)) continue;
                if (TryProcessParagraph(container)) continue;
                _pos++;
            }
            return container;
        }

        // ---------- helpers ----------

        private static bool IsBlank(string line)
        {
            for (int i = 0; i < line.Length; i++)
                if (line[i] != ' ' && line[i] != '\t') return false;
            return true;
        }

        private static int IndentOf(string line)
        {
            int col = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == ' ') col++;
                else if (line[i] == '\t') col += 4 - (col % 4);
                else break;
            }
            return col;
        }

        private static string StripIndent(string line, int columns)
        {
            var sb = new StringBuilder();
            int col = 0;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\t')
                {
                    int adv = 4 - (col % 4);
                    if (col >= columns)
                    {
                        for (int k = 0; k < adv; k++) sb.Append(' ');
                        col += adv;
                    }
                    else if (col + adv <= columns)
                    {
                        col += adv;
                    }
                    else
                    {
                        int extra = columns - col;
                        for (int k = 0; k < adv - extra; k++) sb.Append(' ');
                        col = columns;
                    }
                }
                else if (col >= columns)
                {
                    sb.Append(c);
                    col++;
                }
                else
                {
                    col++;
                }
            }
            return sb.ToString();
        }

        // ---------- block elements ----------

        private bool TryProcessThematicBreak(List<Node> container)
        {
            if (!IsThematicBreak(_lines[_pos])) return false;
            _pos++;
            container.Add(new ThematicBreakBlock());
            return true;
        }

        private static bool IsThematicBreak(string line)
        {
            int col = 0, i = 0;
            while (i < line.Length && col < 4 && (line[i] == ' ' || line[i] == '\t'))
            {
                if (line[i] == ' ') col++;
                else col += 4 - (col % 4);
                i++;
            }
            if (col > 3) return false;
            char marker = '\0';
            int count = 0;
            for (; i < line.Length; i++)
            {
                char c = line[i];
                if (c == ' ' || c == '\t') continue;
                if (c == '-' || c == '*' || c == '_')
                {
                    if (marker == '\0') marker = c;
                    else if (c != marker) return false;
                    count++;
                }
                else return false;
            }
            return count >= 3;
        }

        private bool TryProcessAtx(List<Node> container)
        {
            if (!TryAtx(_lines[_pos], out int level, out string content)) return false;
            _pos++;
            var h = new HeadingBlock(level);
            h.Inlines.AddRange(new InlineParser(content, _references).Parse());
            container.Add(h);
            return true;
        }

        private static bool TryAtx(string line, out int level, out string content)
        {
            level = 0;
            content = null;
            int col = 0, i = 0;
            while (i < line.Length && col < 4 && (line[i] == ' ' || line[i] == '\t'))
            {
                if (line[i] == ' ') col++;
                else col += 4 - (col % 4);
                i++;
            }
            if (col > 3) return false;
            int hashStart = i;
            while (i < line.Length && line[i] == '#') i++;
            int hashes = i - hashStart;
            if (hashes < 1 || hashes > 6) return false;
            if (i < line.Length && line[i] != ' ' && line[i] != '\t') return false;

            string rest = line.Substring(i);
            int hashEnd = rest.Length - 1;
            while (hashEnd >= 0 && rest[hashEnd] == '#') hashEnd--;
            if (hashEnd + 1 < rest.Length)
            {
                if (hashEnd < 0) rest = "";
                else if (rest[hashEnd] == ' ' || rest[hashEnd] == '\t') rest = rest.Substring(0, hashEnd);
            }
            content = rest.Trim();
            level = hashes;
            return true;
        }

        private bool TryProcessFence(List<Node> container)
        {
            if (!TryFence(_lines[_pos], out string fence, out string info)) return false;
            char fenceChar = fence[0];
            int fenceLen = fence.Length;
            _pos++;
            var sb = new StringBuilder();
            while (_pos < _lines.Length)
            {
                string l = _lines[_pos];
                if (IsClosingFence(l, fenceChar, fenceLen))
                {
                    _pos++;
                    break;
                }
                sb.Append(l);
                sb.Append('\n');
                _pos++;
            }
            container.Add(new CodeBlock { IsFenced = true, Info = info, Text = sb.ToString() });
            return true;
        }

        private static bool TryFence(string line, out string fence, out string info)
        {
            fence = null;
            info = null;
            int col = 0, i = 0;
            while (i < line.Length && col < 4 && (line[i] == ' ' || line[i] == '\t'))
            {
                if (line[i] == ' ') col++;
                else col += 4 - (col % 4);
                i++;
            }
            if (col > 3 || i >= line.Length) return false;
            char fch = line[i];
            if (fch != '`' && fch != '~') return false;
            int start = i;
            while (i < line.Length && line[i] == fch) i++;
            int len = i - start;
            if (len < 3) return false;
            string rest = line.Substring(i).Trim();
            if (fch == '`' && rest.IndexOf('`') >= 0) return false;
            fence = new string(fch, len);
            info = rest;
            return true;
        }

        private static bool IsClosingFence(string line, char fenceChar, int openLen)
        {
            int col = 0, i = 0;
            while (i < line.Length && col < 4 && (line[i] == ' ' || line[i] == '\t'))
            {
                if (line[i] == ' ') col++;
                else col += 4 - (col % 4);
                i++;
            }
            if (col > 3 || i >= line.Length || line[i] != fenceChar) return false;
            int start = i;
            while (i < line.Length && line[i] == fenceChar) i++;
            if (i - start < openLen) return false;
            while (i < line.Length && (line[i] == ' ' || line[i] == '\t')) i++;
            return i == line.Length;
        }

        private bool TryProcessIndentedCode(List<Node> container)
        {
            if (IndentOf(_lines[_pos]) < 4) return false;
            var sb = new StringBuilder();
            while (_pos < _lines.Length)
            {
                string l = _lines[_pos];
                int ind = IndentOf(l);
                if (ind >= 4)
                {
                    sb.Append(StripIndent(l, 4));
                    sb.Append('\n');
                    _pos++;
                }
                else if (IsBlank(l))
                {
                    int save = _pos;
                    while (_pos < _lines.Length && IsBlank(_lines[_pos])) _pos++;
                    if (_pos < _lines.Length && IndentOf(_lines[_pos]) >= 4)
                    {
                        for (int i = save; i < _pos; i++) sb.Append('\n');
                    }
                    else
                    {
                        _pos = save;
                        break;
                    }
                }
                else break;
            }
            container.Add(new CodeBlock { Text = sb.ToString() });
            return true;
        }

        private bool TryProcessBlockquote(List<Node> container)
        {
            if (!TryBlockquote(_lines[_pos], out _)) return false;
            var q = new BlockQuoteBlock();
            var collected = new List<string>();
            bool canLazy = false;
            while (_pos < _lines.Length)
            {
                string l = _lines[_pos];
                if (TryBlockquote(l, out string content))
                {
                    collected.Add(content);
                    canLazy = !StartsNewBlock(content);
                    _pos++;
                }
                else if (IsBlank(l))
                {
                    int save = _pos;
                    while (_pos < _lines.Length && IsBlank(_lines[_pos])) _pos++;
                    if (_pos < _lines.Length && TryBlockquote(_lines[_pos], out _))
                    {
                        collected.Add("");
                        canLazy = false;
                    }
                    else
                    {
                        _pos = save;
                        break;
                    }
                }
                else if (canLazy && IsLazyContinuation(l))
                {
                    collected.Add(l);
                    _pos++;
                }
                else break;
            }
            var sub = new BlockParser(collected.ToArray(), _references);
            q.Blocks.AddRange(sub.ParseBlockList());
            container.Add(q);
            return true;
        }

        private static bool TryBlockquote(string line, out string content)
        {
            content = null;
            int col = 0, i = 0;
            while (i < line.Length && col < 4 && (line[i] == ' ' || line[i] == '\t'))
            {
                if (line[i] == ' ') col++;
                else col += 4 - (col % 4);
                i++;
            }
            if (col > 3 || i >= line.Length || line[i] != '>') return false;
            i++;
            if (i < line.Length && (line[i] == ' ' || line[i] == '\t')) i++;
            content = line.Substring(i);
            return true;
        }

        private bool TryProcessList(List<Node> container)
        {
            if (!TryListMarker(_lines[_pos], out ListMarkerInfo first)) return false;
            var list = new ListBlock
            {
                IsOrdered = first.IsOrdered,
                Bullet = first.IsOrdered ? '\0' : first.BulletChar,
                Start = first.Start
            };
            while (_pos < _lines.Length)
            {
                if (!TryListMarker(_lines[_pos], out ListMarkerInfo mi)) break;
                if (!mi.CompatibleWith(first)) break;
                list.Items.Add(CollectListItem(mi));
            }
            container.Add(list);
            return true;
        }

        private ListItemBlock CollectListItem(ListMarkerInfo mi)
        {
            var lines = new List<string>();
            lines.Add(mi.Content);
            _pos++;
            while (_pos < _lines.Length)
            {
                string l = _lines[_pos];
                if (IsBlank(l))
                {
                    int save = _pos;
                    while (_pos < _lines.Length && IsBlank(_lines[_pos])) _pos++;
                    if (_pos < _lines.Length && IndentOf(_lines[_pos]) >= mi.ContentIndent)
                    {
                        lines.Add("");
                        continue;
                    }
                    _pos = save;
                    break;
                }
                int ind = IndentOf(l);
                if (ind >= mi.ContentIndent)
                {
                    lines.Add(StripIndent(l, mi.ContentIndent));
                    _pos++;
                }
                else if (LastLineIsParagraph(lines) && IsLazyContinuation(l))
                {
                    lines.Add(l);
                    _pos++;
                }
                else break;
            }

            var item = new ListItemBlock();
            if (lines.Count > 0 && TryTask(lines[0], out bool checkedTask, out string rest))
            {
                item.IsTask = true;
                item.TaskChecked = checkedTask;
                lines[0] = rest;
            }
            var sub = new BlockParser(lines.ToArray(), _references);
            item.Blocks.AddRange(sub.ParseBlockList());
            return item;
        }

        private static bool TryTask(string line, out bool isChecked, out string rest)
        {
            isChecked = false;
            rest = line;
            if (line.Length < 3 || line[0] != '[') return false;
            char inner = line[1];
            if (inner != ' ' && inner != 'x' && inner != 'X') return false;
            if (line[2] != ']') return false;
            isChecked = inner == 'x' || inner == 'X';
            int i = 3;
            while (i < line.Length && (line[i] == ' ' || line[i] == '\t')) i++;
            rest = line.Substring(i);
            return true;
        }

        private static bool LastLineIsParagraph(List<string> lines)
        {
            if (lines.Count == 0) return false;
            string last = lines[lines.Count - 1];
            if (IsBlank(last)) return false;
            return !StartsNewBlock(last);
        }

        private static bool TryListMarker(string line, out ListMarkerInfo info)
        {
            info = default;
            if (IsThematicBreak(line)) return false;
            int col = 0, i = 0;
            while (i < line.Length && col < 4 && (line[i] == ' ' || line[i] == '\t'))
            {
                if (line[i] == ' ') col++;
                else col += 4 - (col % 4);
                i++;
            }
            if (col > 3) return false;
            int markerStartCol = col;
            int markerLen = 0;
            bool ordered = false;
            char bullet = '\0';
            char delimiter = '.';
            int start = 1;

            if (i < line.Length && (line[i] == '-' || line[i] == '+' || line[i] == '*'))
            {
                bullet = line[i];
                markerLen = 1;
                i++;
                col++;
            }
            else if (i < line.Length && char.IsDigit(line[i]))
            {
                int numStart = i;
                while (i < line.Length && char.IsDigit(line[i]) && i - numStart < 10)
                {
                    i++;
                    col++;
                }
                if (i - numStart > 9) return false;
                if (i < line.Length && (line[i] == '.' || line[i] == ')'))
                {
                    delimiter = line[i];
                    start = int.Parse(line.Substring(numStart, i - numStart));
                    ordered = true;
                    markerLen = i - numStart + 1;
                    i++;
                    col++;
                }
                else return false;
            }
            else return false;

            if (i >= line.Length) return false;
            int padding = 0;
            while (i < line.Length && padding < 5 && (line[i] == ' ' || line[i] == '\t'))
            {
                int adv = line[i] == ' ' ? 1 : 4 - (col % 4);
                padding += adv;
                col += adv;
                i++;
                if (padding >= 5) break;
            }
            if (padding == 0) return false;
            int contentIndent = markerStartCol + markerLen + Math.Min(padding, 4);
            info = new ListMarkerInfo(ordered, bullet, delimiter, start, contentIndent, line.Substring(i));
            return true;
        }

        private bool TryProcessReference(List<Node> container)
        {
            if (!TryReferenceDefinition(_lines[_pos], out LinkReference reference)) return false;
            _references[reference.Label] = reference;
            _pos++;
            return true;
        }

        private static bool TryReferenceDefinition(string line, out LinkReference reference)
        {
            reference = null;
            string t = line.Substring(Math.Min(IndentOf(line), 3));
            if (t.Length < 2 || t[0] != '[') return false;
            int close = t.IndexOf(']');
            if (close <= 0) return false;
            string label = t.Substring(1, close - 1);
            if (label.Length == 0 || label.Length > 999) return false;
            if (label.IndexOf('[') >= 0 || label.IndexOf(']') >= 0) return false;
            int i = close + 1;
            if (i >= t.Length || t[i] != ':') return false;
            i++;
            while (i < t.Length && (t[i] == ' ' || t[i] == '\t')) i++;
            if (i >= t.Length) return false;

            string url;
            string title = null;
            if (t[i] == '<')
            {
                int end = t.IndexOf('>', i + 1);
                if (end < 0) return false;
                url = t.Substring(i + 1, end - i - 1);
                i = end + 1;
            }
            else
            {
                int end = i;
                while (end < t.Length && t[end] != ' ' && t[end] != '\t') end++;
                url = t.Substring(i, end - i);
                i = end;
            }
            while (i < t.Length && (t[i] == ' ' || t[i] == '\t')) i++;
            if (i < t.Length)
            {
                char q = t[i];
                if (q == '"' || q == '\'')
                {
                    int end = t.IndexOf(q, i + 1);
                    if (end < 0) return false;
                    title = t.Substring(i + 1, end - i - 1);
                    i = end + 1;
                }
                else if (q == '(')
                {
                    int end = t.IndexOf(')', i + 1);
                    if (end < 0) return false;
                    title = t.Substring(i + 1, end - i - 1);
                    i = end + 1;
                }
                else return false;
                while (i < t.Length && (t[i] == ' ' || t[i] == '\t')) i++;
                if (i != t.Length) return false;
            }
            reference = new LinkReference(MarkdownUtil.NormalizeLabel(label), MarkdownUtil.Unescape(url), title);
            return true;
        }

        // ---------- paragraph / setext / table ----------

        private bool TryProcessParagraph(List<Node> container)
        {
            var lines = new List<string>();
            while (_pos < _lines.Length)
            {
                string l = _lines[_pos];
                if (IsBlank(l)) break;
                if (StartsNewBlock(l)) break;
                if (lines.Count > 0 && IsSetextUnderline(l, out _)) break;
                lines.Add(l);
                _pos++;
            }
            if (lines.Count == 0) return false;

            if (_pos < _lines.Length && IsSetextUnderline(_lines[_pos], out int level))
            {
                _pos++;
                var h = new HeadingBlock(level);
                h.Inlines.AddRange(new InlineParser(string.Join("\n", lines), _references).Parse());
                container.Add(h);
                return true;
            }

            if (lines.Count == 1 && _pos < _lines.Length && IsTableDelimiter(_lines[_pos], out List<TableAlign> aligns))
            {
                _pos++;
                var table = new TableBlock();
                table.Aligns.AddRange(aligns);
                table.HasHeader = true;
                table.Rows.Add(ParseTableRow(lines[0], aligns.Count));
                while (_pos < _lines.Length)
                {
                    string r = _lines[_pos];
                    if (IsBlank(r) || StartsNewBlock(r) || r.IndexOf('|') < 0) break;
                    table.Rows.Add(ParseTableRow(r, aligns.Count));
                    _pos++;
                }
                container.Add(table);
                return true;
            }

            var p = new ParagraphBlock();
            p.Inlines.AddRange(new InlineParser(string.Join("\n", lines), _references).Parse());
            container.Add(p);
            return true;
        }

        private TableRow ParseTableRow(string line, int alignCount)
        {
            var row = new TableRow();
            var cells = SplitTableRow(line);
            for (int i = 0; i < alignCount; i++)
            {
                string cell = i < cells.Count ? cells[i] : "";
                row.Cells.Add(new InlineParser(cell, _references).Parse());
            }
            return row;
        }

        private static List<string> SplitTableRow(string line)
        {
            line = line.Trim();
            if (line.StartsWith("|")) line = line.Substring(1);
            var parts = new List<string>();
            var sb = new StringBuilder();
            bool esc = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (esc)
                {
                    sb.Append(c);
                    esc = false;
                }
                else if (c == '\\')
                {
                    sb.Append(c);
                    esc = true;
                }
                else if (c == '|')
                {
                    parts.Add(sb.ToString().Trim());
                    sb.Clear();
                }
                else sb.Append(c);
            }
            parts.Add(sb.ToString().Trim());
            if (parts.Count > 0 && parts[parts.Count - 1].Length == 0 && line.EndsWith("|"))
                parts.RemoveAt(parts.Count - 1);
            return parts;
        }

        private static bool IsTableDelimiter(string line, out List<TableAlign> aligns)
        {
            aligns = null;
            string t = line.Trim();
            if (t.Length == 0 || t.IndexOf('-') < 0) return false;
            var cells = SplitTableRow(t);
            if (cells.Count == 0) return false;
            var result = new List<TableAlign>();
            foreach (var raw in cells)
            {
                string c = raw.Trim();
                if (c.Length == 0) return false;
                bool left = c.StartsWith(":");
                bool right = c.EndsWith(":");
                string core = c.Trim(':');
                if (core.Length == 0 || core.Any(ch => ch != '-')) return false;
                result.Add(left && right ? TableAlign.Center : left ? TableAlign.Left : right ? TableAlign.Right : TableAlign.None);
            }
            aligns = result;
            return true;
        }

        private static bool IsSetextUnderline(string line, out int level)
        {
            level = 0;
            string t = line.Trim();
            if (t.Length == 0) return false;
            char marker = t[0];
            if (marker != '=' && marker != '-') return false;
            for (int i = 1; i < t.Length; i++)
                if (t[i] != marker) return false;
            level = marker == '=' ? 1 : 2;
            return true;
        }

        private static bool StartsNewBlock(string line)
        {
            if (IsBlank(line)) return false;
            if (IndentOf(line) >= 4) return true;
            string t = line.Substring(Math.Min(IndentOf(line), 3));
            if (t.Length == 0) return false;
            if (t[0] == '>') return true;
            if (TryAtx(line, out _, out _)) return true;
            if (IsThematicBreak(line)) return true;
            if (TryFence(line, out _, out _)) return true;
            if (TryListMarker(line, out _)) return true;
            return false;
        }

        private static bool IsLazyContinuation(string line)
        {
            if (IsBlank(line)) return false;
            if (IndentOf(line) >= 4) return false;
            string t = line.Substring(Math.Min(IndentOf(line), 3));
            if (t.Length == 0) return false;
            if (TryAtx(line, out _, out _)) return false;
            if (IsThematicBreak(line)) return false;
            if (TryFence(line, out _, out _)) return false;
            if (TryListMarker(line, out _)) return false;
            if (t[0] == '>') return false;
            return true;
        }
    }
}