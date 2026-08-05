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
        public PLine Content;

        public ListMarkerInfo(bool ordered, char bullet, char delimiter, int start, int contentIndent, PLine content)
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

    internal struct PLine
    {
        public string Text;
        public int Offset;
        public bool IsLazy;

        public PLine(string text, int offset)
        {
            Text = text;
            Offset = offset;
            IsLazy = false;
        }

        public PLine(string text, int offset, bool isLazy)
        {
            Text = text;
            Offset = offset;
            IsLazy = isLazy;
        }
    }

    internal sealed class BlockParser
    {
        private readonly PLine[] _lines;
        private int _pos;
        private bool _lastBlankLine;
        private bool _curWasBlank;
        private readonly Dictionary<string, LinkReference> _references;

        public BlockParser(string text)
        {
            var raw = Normalize(text);
            _lines = new PLine[raw.Length];
            for (int i = 0; i < raw.Length; i++) _lines[i] = new PLine(raw[i], 0);
            _references = new Dictionary<string, LinkReference>();
        }

        private BlockParser(PLine[] lines, Dictionary<string, LinkReference> references)
        {
            _lines = lines;
            _references = references;
        }

        public static string[] Normalize(string text)
        {
            if (text == null) return new string[0];
            text = text.Replace("\r\n", "\n").Replace('\r', '\n');
            var lines = new List<string>(text.Split('\n'));
            if (lines.Count > 0 && lines[lines.Count - 1].Length == 0) lines.RemoveAt(lines.Count - 1);
            return lines.ToArray();
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
            bool inFence = false;
            char fenceChar = '\0';
            for (int i = 0; i < _lines.Length; i++)
            {
                string line = _lines[i].Text;
                if (inFence)
                {
                    if (IsClosingFence(line, fenceChar, 3)) inFence = false;
                    continue;
                }
                if (TryFence(line, out string f, out _))
                {
                    inFence = true;
                    fenceChar = f[0];
                    continue;
                }
                if (IndentOfText(line) >= 4) continue;
                if (IsBlankText(line)) continue;
                if (TryReferenceDefinition(line, out LinkReference r) && !_references.ContainsKey(r.Label))
                    _references[r.Label] = r;
            }
        }

        public List<Node> ParseBlockList()
        {
            var container = new List<Node>();
            while (_pos < _lines.Length)
            {
                PLine line = _lines[_pos];
                if (IsBlank(line))
                {
                    _lastBlankLine = true;
                    _pos++;
                    continue;
                }
                _curWasBlank = _lastBlankLine;
                _lastBlankLine = false;
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

        private static bool IsBlank(PLine line)
        {
            for (int i = 0; i < line.Text.Length; i++)
                if (line.Text[i] != ' ' && line.Text[i] != '\t') return false;
            return true;
        }

        private static bool IsBlankText(string s)
        {
            for (int i = 0; i < s.Length; i++)
                if (s[i] != ' ' && s[i] != '\t') return false;
            return true;
        }

        private static int IndentOf(PLine line)
        {
            int col = 0;
            string s = line.Text;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == ' ') col++;
                else if (s[i] == '\t') col += 4 - ((line.Offset + col) % 4);
                else break;
            }
            return col;
        }

        private static PLine SliceByColumn(PLine line, int column)
        {
            var sb = new StringBuilder();
            int col = line.Offset;
            string s = line.Text;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\t')
                {
                    int adv = 4 - (col % 4);
                    if (col >= column)
                    {
                        sb.Append(c);
                        col += adv;
                    }
                    else if (col + adv <= column)
                    {
                        col += adv;
                    }
                    else
                    {
                        int extra = column - col;
                        for (int k = 0; k < adv - extra; k++) sb.Append(' ');
                        col = column;
                    }
                }
                else if (col >= column)
                {
                    sb.Append(c);
                    col++;
                }
                else
                {
                    col++;
                }
            }
            return new PLine(sb.ToString(), column);
        }

        // ---------- block elements ----------

        private bool TryProcessThematicBreak(List<Node> container)
        {
            if (!IsThematicBreak(_lines[_pos].Text)) return false;
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
            if (!TryAtx(_lines[_pos].Text, out int level, out string content)) return false;
            _pos++;
            var h = new HeadingBlock(level);
            h.Inlines.AddRange(new InlineParser(content, _references, true).Parse());
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
            PLine openLine = _lines[_pos];
            if (!TryFence(openLine.Text, out string fence, out string info)) return false;
            int fenceIndent = IndentOf(openLine);
            char fenceChar = fence[0];
            int fenceLen = fence.Length;
            _pos++;
            var sb = new StringBuilder();
            while (_pos < _lines.Length)
            {
                PLine l = _lines[_pos];
                if (IsClosingFence(l.Text, fenceChar, fenceLen))
                {
                    _pos++;
                    break;
                }
                int strip = Math.Min(fenceIndent, IndentOf(l));
                sb.Append(SliceByColumn(l, l.Offset + strip).Text);
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
            if (!_curWasBlank && container.Count > 0 && container[container.Count - 1] is ParagraphBlock prevPara)
            {
                prevPara.RawLines.Add(_lines[_pos].Text);
                prevPara.Inlines.Clear();
                prevPara.Inlines.AddRange(new InlineParser(string.Join("\n", prevPara.RawLines), _references, true).Parse());
                _pos++;
                return true;
            }
            var sb = new StringBuilder();
            while (_pos < _lines.Length)
            {
                PLine l = _lines[_pos];
                int ind = IndentOf(l);
                if (ind >= 4)
                {
                    sb.Append(SliceByColumn(l, l.Offset + 4).Text);
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
            var collected = new List<PLine>();
            bool canLazy = false;
            while (_pos < _lines.Length)
            {
                PLine l = _lines[_pos];
                if (TryBlockquote(l, out PLine content))
                {
                    collected.Add(content);
                    canLazy = content.Text.Length > 0 && !StartsNewBlock(content);
                    _pos++;
                }
                else if (IsBlank(l))
                {
                    int save = _pos;
                    while (_pos < _lines.Length && IsBlank(_lines[_pos])) _pos++;
                    _pos = save;
                    break;
                }
                else if (canLazy && IsLazyContinuation(l))
                {
                    collected.Add(new PLine(l.Text, l.Offset, true));
                    _pos++;
                }
                else break;
            }
            var sub = new BlockParser(collected.ToArray(), _references);
            q.Blocks.AddRange(sub.ParseBlockList());
            container.Add(q);
            return true;
        }

        private static bool TryBlockquote(PLine line, out PLine content)
        {
            content = default;
            int col = line.Offset, i = 0;
            string s = line.Text;
            while (i < s.Length && col < line.Offset + 4 && (s[i] == ' ' || s[i] == '\t'))
            {
                if (s[i] == ' ') col++;
                else col += 4 - (col % 4);
                i++;
            }
            if (col > line.Offset + 3 || i >= s.Length || s[i] != '>') return false;
            i++;
            col++;
            if (i < s.Length && (s[i] == ' ' || s[i] == '\t')) { i++; col++; }
            content = SliceByColumn(line, col);
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
                if (IsBlank(_lines[_pos]))
                {
                    int save = _pos;
                    while (_pos < _lines.Length && IsBlank(_lines[_pos])) _pos++;
                    if (_pos < _lines.Length && TryListMarker(_lines[_pos], out ListMarkerInfo nm) && nm.CompatibleWith(first))
                    {
                        list.IsLoose = true;
                        list.Items.Add(CollectListItem(nm, list));
                        continue;
                    }
                    _pos = save;
                    break;
                }
                if (!TryListMarker(_lines[_pos], out ListMarkerInfo mi)) break;
                if (!mi.CompatibleWith(first)) break;
                list.Items.Add(CollectListItem(mi, list));
            }
            container.Add(list);
            return true;
        }

        private ListItemBlock CollectListItem(ListMarkerInfo mi, ListBlock list)
        {
            var lines = new List<PLine>();
            lines.Add(mi.Content);
            _pos++;
            while (_pos < _lines.Length)
            {
                PLine l = _lines[_pos];
                if (IsBlank(l))
                {
                    int save = _pos;
                    while (_pos < _lines.Length && IsBlank(_lines[_pos])) _pos++;
                    if (!ItemIsEmpty(lines) && _pos < _lines.Length && _lines[_pos].Offset + IndentOf(_lines[_pos]) >= mi.ContentIndent)
                    {
                        list.IsLoose = true;
                        lines.Add(new PLine("", l.Offset));
                        continue;
                    }
                    _pos = save;
                    break;
                }
                int ind = l.Offset + IndentOf(l);
                if (ind >= mi.ContentIndent)
                {
                    lines.Add(SliceByColumn(l, mi.ContentIndent));
                    _pos++;
                }
                else if (ItemIsEmpty(lines) && IsLazyContinuation(l))
                {
                    lines.Add(l);
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
            if (lines.Count > 0 && TryTask(lines[0].Text, out bool checkedTask, out string rest))
            {
                item.IsTask = true;
                item.TaskChecked = checkedTask;
                lines[0] = new PLine(rest, lines[0].Offset + 3);
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

        private static bool LastLineIsParagraph(List<PLine> lines)
        {
            if (lines.Count == 0) return false;
            PLine last = lines[lines.Count - 1];
            if (IsBlank(last)) return false;
            return !StartsNewBlock(last);
        }

        private static bool ItemIsEmpty(List<PLine> lines)
        {
            for (int i = 0; i < lines.Count; i++)
                if (!IsBlank(lines[i])) return false;
            return true;
        }

        private static bool TryListMarker(PLine line, out ListMarkerInfo info)
        {
            info = default;
            if (IsThematicBreak(line.Text)) return false;
            int col = line.Offset, i = 0;
            string s = line.Text;
            while (i < s.Length && col < line.Offset + 4 && (s[i] == ' ' || s[i] == '\t'))
            {
                if (s[i] == ' ') col++;
                else col += 4 - (col % 4);
                i++;
            }
            if (col > line.Offset + 3) return false;
            int markerStartCol = col;
            int markerLen = 0;
            bool ordered = false;
            char bullet = '\0';
            char delimiter = '.';
            int start = 1;

            if (i < s.Length && (s[i] == '-' || s[i] == '+' || s[i] == '*'))
            {
                bullet = s[i];
                markerLen = 1;
                i++;
                col++;
            }
            else if (i < s.Length && char.IsDigit(s[i]))
            {
                int numStart = i;
                while (i < s.Length && char.IsDigit(s[i]) && i - numStart < 10)
                {
                    i++;
                    col++;
                }
                if (i - numStart > 9) return false;
                if (i < s.Length && (s[i] == '.' || s[i] == ')'))
                {
                    delimiter = s[i];
                    start = int.Parse(s.Substring(numStart, i - numStart));
                    ordered = true;
                    markerLen = i - numStart + 1;
                    i++;
                    col++;
                }
                else return false;
            }
            else return false;

            bool bareItem = i >= s.Length;
            if (!bareItem && s[i] != ' ' && s[i] != '\t') return false;
            int markerEndCol = col;
            int spacesAfter = 0;
            int endIdx = i;
            while (endIdx < s.Length && spacesAfter < 5)
            {
                char sc = s[endIdx];
                if (sc == ' ')
                {
                    markerEndCol++;
                    endIdx++;
                    spacesAfter++;
                }
                else if (sc == '\t')
                {
                    int adv = 4 - (markerEndCol % 4);
                    markerEndCol += adv;
                    endIdx++;
                    spacesAfter += adv;
                }
                else break;
            }
            bool blankItem = endIdx >= s.Length;
            if (spacesAfter >= 5 || spacesAfter < 1 || blankItem)
            {
                int contentIndent = markerStartCol + markerLen + 1;
                info = new ListMarkerInfo(ordered, bullet, delimiter, start, contentIndent,
                    SliceByColumn(line, contentIndent));
            }
            else
            {
                int contentIndent = markerStartCol + markerLen + spacesAfter;
                info = new ListMarkerInfo(ordered, bullet, delimiter, start, contentIndent,
                    SliceByColumn(line, contentIndent));
            }
            return true;
        }

        private bool TryProcessReference(List<Node> container)
        {
            if (container.Count > 0 && container[container.Count - 1] is ParagraphBlock && !_curWasBlank) return false;
            int start = _pos;
            if (ParseReferenceDefinition(out LinkReference reference, out int consumed))
            {
                if (!_references.ContainsKey(reference.Label)) _references[reference.Label] = reference;
                _pos += consumed;
                _lastBlankLine = true;
                return true;
            }
            _pos = start;
            return false;
        }

        private bool ParseReferenceDefinition(out LinkReference reference, out int consumed)
        {
            reference = null;
            consumed = 1;
            string t = _lines[_pos].Text;
            t = t.Substring(Math.Min(IndentOfText(t), 3));
            if (t.Length < 2 || t[0] != '[') return false;
            int close = -1;
            for (int j = 1; j < t.Length; j++)
            {
                if (t[j] == '\\' && j + 1 < t.Length) { j++; continue; }
                if (t[j] == ']') { close = j; break; }
            }
            if (close <= 0) return false;
            string label = t.Substring(1, close - 1);
            if (label.Length == 0 || label.Length > 999 || label.IndexOf('[') >= 0) return false;
            int i = close + 1;
            if (i >= t.Length || t[i] != ':') return false;
            i++;
            while (i < t.Length && (t[i] == ' ' || t[i] == '\t')) i++;

            string url = null;
            string title = null;
            int lineIdx = 0;
            if (i >= t.Length)
            {
                lineIdx = 1;
                if (_pos + 1 >= _lines.Length) return false;
                t = _lines[_pos + 1].Text.Trim();
                i = 0;
                consumed++;
            }
            if (i < t.Length)
            {
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
            }
            bool titleSpace = false;
            while (i < t.Length && (t[i] == ' ' || t[i] == '\t')) { i++; titleSpace = true; }
            if (i < t.Length)
            {
                char q = t[i];
                if (!ParseTitle(t, ref i, q, titleSpace, out title)) return false;
            }
            else if (lineIdx < 2 && _pos + 2 < _lines.Length && !IsBlankText(_lines[_pos + 2].Text))
            {
                string t3 = _lines[_pos + 2].Text.Trim();
                if (t3.Length > 0 && (t3[0] == '"' || t3[0] == '\'' || t3[0] == '('))
                {
                    int ti = 0;
                    if (ParseTitle(t3, ref ti, t3[0], true, out title))
                    {
                        consumed++;
                        lineIdx = 2;
                    }
                }
            }
            reference = new LinkReference(MarkdownUtil.NormalizeLabel(label), MarkdownUtil.Unescape(url), title);
            return true;
        }

        private static bool ParseTitle(string t, ref int i, char q, bool titleSpace, out string title)
        {
            title = null;
            if (q == '(' && !titleSpace) return false;
            if (q != '"' && q != '\'' && q != '(') return false;
            char closeQ = q == '(' ? ')' : q;
            int end = -1;
            for (int j = i + 1; j < t.Length; j++)
            {
                if (t[j] == '\\' && j + 1 < t.Length) { j++; continue; }
                if (t[j] == closeQ) { end = j; break; }
            }
            if (end < 0) return false;
            title = t.Substring(i + 1, end - i - 1);
            i = end + 1;
            while (i < t.Length && (t[i] == ' ' || t[i] == '\t')) i++;
            return i == t.Length;
        }

        private static bool TryReferenceDefinition(string line, out LinkReference reference)
        {
            reference = null;
            string t = line.Substring(Math.Min(IndentOfText(line), 3));
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
            bool titleSpace = false;
            while (i < t.Length && (t[i] == ' ' || t[i] == '\t')) { i++; titleSpace = true; }
            if (i < t.Length)
            {
                char q = t[i];
                if (q == '(' && !titleSpace) return false;
                if (q == '"' || q == '\'')
                {
                    int end = -1;
                    for (int j = i + 1; j < t.Length; j++)
                    {
                        if (t[j] == '\\' && j + 1 < t.Length) { j++; continue; }
                        if (t[j] == q) { end = j; break; }
                    }
                    if (end < 0) return false;
                    title = t.Substring(i + 1, end - i - 1);
                    i = end + 1;
                }
                else if (q == '(')
                {
                    int end = -1;
                    for (int j = i + 1; j < t.Length; j++)
                    {
                        if (t[j] == '\\' && j + 1 < t.Length) { j++; continue; }
                        if (t[j] == ')') { end = j; break; }
                    }
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

        private static int IndentOfText(string s)
        {
            int col = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == ' ') col++;
                else if (s[i] == '\t') col += 4 - (col % 4);
                else break;
            }
            return col;
        }

        // ---------- paragraph / setext / table ----------

        private bool TryProcessParagraph(List<Node> container)
        {
            var lines = new List<PLine>();
            while (_pos < _lines.Length)
            {
                PLine l = _lines[_pos];
                if (IsBlank(l)) break;
                if (lines.Count > 0 && IndentOf(l) >= 4)
                {
                    lines.Add(l);
                    _pos++;
                    continue;
                }
                if (lines.Count > 0 && TryListMarker(l, out ListMarkerInfo lm2) && lm2.IsOrdered && lm2.Start != 1)
                {
                    lines.Add(l);
                    _pos++;
                    continue;
                }
                if (lines.Count > 0 && TryListMarker(l, out ListMarkerInfo lm3) && IsBlank(lm3.Content))
                {
                    lines.Add(l);
                    _pos++;
                    continue;
                }
                if (StartsNewBlock(l)) break;
                if (lines.Count > 0 && IsSetextUnderline(l.Text, out _) && !l.IsLazy) break;
                if (lines.Count > 0 && IsTableDelimiter(l.Text, out _)) break;
                lines.Add(l);
                _pos++;
            }
            if (lines.Count == 0) return false;

            if (_pos < _lines.Length && IsSetextUnderline(_lines[_pos].Text, out int level))
            {
                _pos++;
                var h = new HeadingBlock(level);
                h.Inlines.AddRange(new InlineParser(JoinLines(lines), _references, true).Parse());
                container.Add(h);
                return true;
            }

            if (lines.Count == 1 && _pos < _lines.Length && IsTableDelimiter(_lines[_pos].Text, out List<TableAlign> aligns))
            {
                _pos++;
                var table = new TableBlock();
                table.Aligns.AddRange(aligns);
                table.HasHeader = true;
                table.Rows.Add(ParseTableRow(lines[0].Text, aligns.Count));
                while (_pos < _lines.Length)
                {
                    PLine r = _lines[_pos];
                    if (IsBlank(r) || StartsNewBlock(r) || r.Text.IndexOf('|') < 0) break;
                    table.Rows.Add(ParseTableRow(r.Text, aligns.Count));
                    _pos++;
                }
                container.Add(table);
                return true;
            }

            var p = new ParagraphBlock();
            foreach (var l in lines) p.RawLines.Add(l.Text);
            p.Inlines.AddRange(new InlineParser(JoinLines(lines), _references, true).Parse());
            container.Add(p);
            return true;
        }

        private static string JoinLines(List<PLine> lines)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(lines[i].Text);
            }
            return sb.ToString();
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

        private static bool StartsNewBlock(PLine line)
        {
            if (IsBlank(line)) return false;
            if (IndentOf(line) >= 4) return true;
            string t = line.Text.Substring(Math.Min(IndentOf(line), Math.Min(3, line.Text.Length)));
            if (t.Length == 0) return false;
            if (t[0] == '>') return true;
            if (TryAtx(line.Text, out _, out _)) return true;
            if (IsThematicBreak(line.Text)) return true;
            if (TryFence(line.Text, out _, out _)) return true;
            if (TryListMarker(line, out _)) return true;
            return false;
        }

        private static bool IsLazyContinuation(PLine line)
        {
            if (IsBlank(line)) return false;
            string t = line.Text.Substring(Math.Min(IndentOf(line), Math.Min(3, line.Text.Length)));
            if (t.Length == 0) return false;
            if (TryAtx(line.Text, out _, out _)) return false;
            if (IsThematicBreak(line.Text)) return false;
            if (TryFence(line.Text, out _, out _)) return false;
            if (TryListMarker(line, out _)) return false;
            if (t[0] == '>') return false;
            return true;
        }
    }
}