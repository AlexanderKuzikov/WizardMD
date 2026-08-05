using System.Collections.Generic;
using WizardMD.Core.Ast;

namespace WizardMD.Core
{
    internal sealed class DelimiterNode : InlineNode
    {
        public char Char;
        public int Length;
        public bool CanOpen;
        public bool CanClose;
    }

    internal sealed class InlineParser
    {
        private readonly string _text;
        private readonly Dictionary<string, LinkReference> _references;
        private readonly bool _trimLineWhitespace;

        public InlineParser(string text, Dictionary<string, LinkReference> references)
            : this(text, references, false)
        {
        }

        public InlineParser(string text, Dictionary<string, LinkReference> references, bool trimLineWhitespace)
        {
            _text = text ?? "";
            _references = references ?? new Dictionary<string, LinkReference>();
            _trimLineWhitespace = trimLineWhitespace;
        }

        public List<InlineNode> Parse()
        {
            var nodes = new List<InlineNode>();
            var sb = new System.Text.StringBuilder();
            int i = 0;
            bool skipLeading = _trimLineWhitespace;
            while (i < _text.Length)
            {
                char c = _text[i];
                if (skipLeading && (c == ' ' || c == '\t'))
                {
                    i++;
                    continue;
                }
                skipLeading = false;
                if (c == '\\')
                {
                    if (i + 1 < _text.Length && _text[i + 1] == '\n')
                    {
                        Flush(sb, nodes);
                        nodes.Add(new HardBreakNode());
                        i += 2;
                    }
                    else if (i + 1 < _text.Length && MarkdownUtil.IsAsciiPunctuation(_text[i + 1]))
                    {
                        sb.Append(_text[i + 1]);
                        i += 2;
                    }
                    else
                    {
                        sb.Append(c);
                        i++;
                    }
                }
                else if (c == '`')
                {
                    Flush(sb, nodes);
                    EmitCodeSpan(i, nodes, out i);
                }
                else if (c == '\n')
                {
                    int sp = 0;
                    while (sb.Length - sp > 0 && sb[sb.Length - 1 - sp] == ' ') sp++;
                    if (sp >= 2)
                    {
                        sb.Length -= sp;
                        Flush(sb, nodes);
                        nodes.Add(new HardBreakNode());
                    }
                    else
                    {
                        if (sp == 1) sb.Length--;
                        Flush(sb, nodes);
                        nodes.Add(new SoftBreakNode());
                    }
                    skipLeading = _trimLineWhitespace;
                    i++;
                }
                else if (c == '!' && i + 1 < _text.Length && _text[i + 1] == '[')
                {
                    Flush(sb, nodes);
                    EmitLinkOrImage(i + 1, true, nodes, out i);
                }
                else if (c == '[')
                {
                    Flush(sb, nodes);
                    EmitLinkOrImage(i, false, nodes, out i);
                }
                else if (c == '<')
                {
                    Flush(sb, nodes);
                    EmitAutolink(i, nodes, out i);
                }
                else if (c == '&')
                {
                    if (MarkdownUtil.TryEntity(_text, i, out string val, out int entEnd))
                    {
                        sb.Append(val);
                        i = entEnd;
                    }
                    else
                    {
                        sb.Append(c);
                        i++;
                    }
                }
                else if (c == '*' || c == '_' || c == '~')
                {
                    int runStart = i;
                    while (i < _text.Length && _text[i] == c) i++;
                    int len = i - runStart;
                    Flush(sb, nodes);
                    if (c == '~')
                    {
                        if (len >= 2)
                            nodes.Add(MakeDelimiter(c, len, runStart, i));
                        else
                            nodes.Add(new TextNode(new string(c, len)));
                    }
                    else
                    {
                        nodes.Add(MakeDelimiter(c, len, runStart, i));
                    }
                }
                else
                {
                    sb.Append(c);
                    i++;
                }
            }
            Flush(sb, nodes);
            return ProcessDelimiters(nodes);
        }

        private static void Flush(System.Text.StringBuilder sb, List<InlineNode> nodes)
        {
            if (sb.Length > 0)
            {
                nodes.Add(new TextNode(sb.ToString()));
                sb.Clear();
            }
        }

        private DelimiterNode MakeDelimiter(char ch, int len, int runStart, int runEnd)
        {
            char before = runStart > 0 ? _text[runStart - 1] : '\n';
            char after = runEnd < _text.Length ? _text[runEnd] : '\n';

            bool beforeWs = MarkdownUtil.IsWhitespace(before);
            bool beforePunct = MarkdownUtil.IsAsciiPunctuation(before);
            bool afterWs = MarkdownUtil.IsWhitespace(after);
            bool afterPunct = MarkdownUtil.IsAsciiPunctuation(after);

            bool leftFlanking = !afterWs && (!afterPunct || beforeWs || beforePunct);
            bool rightFlanking = !beforeWs && (!beforePunct || afterWs || afterPunct);
            if (ch == '_')
            {
                bool bothAlnum = MarkdownUtil.IsAlnum(before) && MarkdownUtil.IsAlnum(after);
                if (bothAlnum)
                {
                    leftFlanking = false;
                    rightFlanking = false;
                }
            }
            return new DelimiterNode { Char = ch, Length = len, CanOpen = leftFlanking, CanClose = rightFlanking };
        }

        private void EmitCodeSpan(int start, List<InlineNode> nodes, out int newPos)
        {
            int i = start;
            while (i < _text.Length && _text[i] == '`') i++;
            int ticks = i - start;
            int j = i;
            while (j < _text.Length)
            {
                if (_text[j] == '`')
                {
                    int k = j;
                    while (k < _text.Length && _text[k] == '`') k++;
                    if (k - j == ticks)
                    {
                        string content = _text.Substring(i, j - i).Replace('\n', ' ');
                        if (content.Length >= 2 && content[0] == ' ' && content[content.Length - 1] == ' '
                            && content.Trim().Length > 0)
                        {
                            content = content.Substring(1, content.Length - 2);
                        }
                        nodes.Add(new CodeNode(content));
                        newPos = k;
                        return;
                    }
                    j = k;
                }
                else j++;
            }
            nodes.Add(new TextNode(new string('`', ticks)));
            newPos = i;
        }

        private void EmitLinkOrImage(int bracketPos, bool isImage, List<InlineNode> nodes, out int newPos)
        {
            int close = FindClosingBracket(bracketPos);
            if (close < 0)
            {
                nodes.Add(new TextNode(isImage ? "![" : "["));
                newPos = bracketPos + (isImage ? 2 : 1);
                return;
            }
            string text = _text.Substring(bracketPos + 1, close - bracketPos - 1);
            int after = close + 1;

            if (after < _text.Length && _text[after] == '(')
            {
                if (TryParseLinkDest(after + 1, out string url, out string title, out int destEnd)
                    && destEnd < _text.Length && _text[destEnd] == ')')
                {
                    newPos = destEnd + 1;
                    EmitLinkNode(nodes, isImage, url, title, text);
                    return;
                }
                if (TryShortcutLink(text, isImage, nodes, out int sp))
                {
                    newPos = sp;
                    return;
                }
            }
            else if (after < _text.Length && _text[after] == '[')
            {
                int close2 = _text.IndexOf(']', after + 1);
                if (close2 > after)
                {
                    string label = _text.Substring(after + 1, close2 - after - 1);
                    if (label.Length == 0) label = text;
                    string norm = MarkdownUtil.NormalizeLabel(label);
                    if (_references.TryGetValue(norm, out LinkReference rf))
                    {
                        newPos = close2 + 1;
                        EmitLinkNode(nodes, isImage, rf.Url, rf.Title, text);
                        return;
                    }
                }
            }
            else
            {
                string norm = MarkdownUtil.NormalizeLabel(text);
                if (_references.TryGetValue(norm, out LinkReference rf))
                {
                    newPos = close + 1;
                    EmitLinkNode(nodes, isImage, rf.Url, rf.Title, text);
                    return;
                }
            }

            nodes.Add(new TextNode(isImage ? "![" : "["));
            newPos = bracketPos + (isImage ? 2 : 1);
        }

        private bool TryShortcutLink(string text, bool isImage, List<InlineNode> nodes, out int newPos)
        {
            newPos = 0;
            string norm = MarkdownUtil.NormalizeLabel(text);
            if (!_references.TryGetValue(norm, out LinkReference rf)) return false;
            EmitLinkNode(nodes, isImage, rf.Url, rf.Title, text);
            return true;
        }

        private int FindClosingBracket(int open)
        {
            int depth = 0;
            for (int j = open; j < _text.Length; j++)
            {
                char c = _text[j];
                if (c == '\\' && j + 1 < _text.Length)
                {
                    j++;
                    continue;
                }
                if (c == '[') depth++;
                else if (c == ']')
                {
                    depth--;
                    if (depth == 0) return j;
                }
            }
            return -1;
        }

        private void EmitLinkNode(List<InlineNode> nodes, bool isImage, string url, string title, string text)
        {
            string cleanUrl = MarkdownUtil.NormalizeUrl(MarkdownUtil.Unescape(url));
            string cleanTitle = title == null ? "" : MarkdownUtil.DecodeEntities(MarkdownUtil.Unescape(title));
            if (isImage)
            {
                var img = new ImageNode { Url = cleanUrl, Title = cleanTitle };
                img.Children.AddRange(new InlineParser(PlainText(text), null).Parse());
                nodes.Add(img);
            }
            else
            {
                var link = new LinkNode { Url = cleanUrl, Title = cleanTitle };
                link.Children.AddRange(new InlineParser(text, _references).Parse());
                nodes.Add(link);
            }
        }

        private static string PlainText(string s)
        {
            return s.Replace("[", "").Replace("]", "");
        }

        private bool TryParseLinkDest(int start, out string url, out string title, out int end)
        {
            url = null;
            title = null;
            int i = start;
            while (i < _text.Length && (_text[i] == ' ' || _text[i] == '\n')) i++;
            if (i >= _text.Length)
            {
                end = i;
                return false;
            }
            if (_text[i] == '<')
            {
                int close = -1;
                for (int j = i + 1; j < _text.Length; j++)
                {
                    if (_text[j] == '\n') { end = j; return false; }
                    if (_text[j] == '\\' && j + 1 < _text.Length) { j++; continue; }
                    if (_text[j] == '>') { close = j; break; }
                }
                if (close < 0)
                {
                    end = i;
                    return false;
                }
                url = _text.Substring(i + 1, close - i - 1);
                i = close + 1;
            }
            else
            {
                int depth = 0;
                int startUrl = i;
                while (i < _text.Length)
                {
                    char _c = _text[i];
                    if (_c == '\\' && i + 1 < _text.Length)
                    {
                        i += 2;
                        continue;
                    }
                    if (_c == '(')
                    {
                        depth++;
                        i++;
                    }
                    else if (_c == ')')
                    {
                        if (depth == 0) break;
                        depth--;
                        i++;
                    }
                    else if (_c == ' ' || _c == '\t' || _c == '\n')
                    {
                        break;
                    }
                    else
                    {
                        i++;
                    }
                }
                url = _text.Substring(startUrl, i - startUrl);
            }

            while (i < _text.Length && (_text[i] == ' ' || _text[i] == '\n')) i++;
            if (i < _text.Length && _text[i] != ')')
            {
                char q = _text[i];
                if (q != '"' && q != '\'' && q != '(')
                {
                    end = i;
                    return false;
                }
                char closeQ = q == '(' ? ')' : q;
                int close = -1;
                for (int j = i + 1; j < _text.Length; j++)
                {
                    if (_text[j] == '\\' && j + 1 < _text.Length) { j++; continue; }
                    if (_text[j] == closeQ) { close = j; break; }
                }
                if (close < 0)
                {
                    end = i;
                    return false;
                }
                title = _text.Substring(i + 1, close - i - 1);
                i = close + 1;
                while (i < _text.Length && (_text[i] == ' ' || _text[i] == '\n')) i++;
            }
            end = i;
            return i < _text.Length && _text[i] == ')';
        }

        private void EmitAutolink(int lt, List<InlineNode> nodes, out int newPos)
        {
            int close = _text.IndexOf('>', lt + 1);
            if (close < 0)
            {
                nodes.Add(new TextNode("<"));
                newPos = lt + 1;
                return;
            }
            string inner = _text.Substring(lt + 1, close - lt - 1);
            int colon = inner.IndexOf(':');
            if (colon > 0 && IsValidScheme(inner.Substring(0, colon)))
            {
                string clean = MarkdownUtil.NormalizeUrl(inner);
                nodes.Add(new AutoLinkNode { Url = clean, Label = inner });
                newPos = close + 1;
                return;
            }
            if (IsValidEmail(inner))
            {
                nodes.Add(new AutoLinkNode { Url = "mailto:" + inner, Label = inner });
                newPos = close + 1;
                return;
            }
            nodes.Add(new TextNode("<"));
            newPos = lt + 1;
        }

        private static bool IsValidScheme(string scheme)
        {
            if (scheme.Length == 0) return false;
            if (!char.IsLetter(scheme[0])) return false;
            for (int i = 1; i < scheme.Length; i++)
            {
                char c = scheme[i];
                if (!char.IsLetterOrDigit(c) && c != '+' && c != '-' && c != '.') return false;
            }
            return true;
        }

        private static bool IsValidEmail(string s)
        {
            int at = s.IndexOf('@');
            if (at <= 0 || at == s.Length - 1) return false;
            if (s.IndexOf('@', at + 1) >= 0) return false;
            foreach (char c in s)
            {
                if (char.IsLetterOrDigit(c) || c == '.' || c == '+' || c == '_' || c == '-' || c == '!'
                    || c == '#' || c == '$' || c == '%' || c == '&' || c == '\'' || c == '*' || c == '/'
                    || c == '=' || c == '?' || c == '^' || c == '`' || c == '{' || c == '|' || c == '}' || c == '~')
                    continue;
                return false;
            }
            return true;
        }

        private static List<InlineNode> ProcessDelimiters(List<InlineNode> input)
        {
            var result = new List<InlineNode>(input);
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int close = 0; close < result.Count; close++)
                {
                    if (!(result[close] is DelimiterNode d) || !d.CanClose) continue;
                    DelimiterNode foundOpen = null;
                    int openIdx = -1;
                    for (int open = close - 1; open >= 0; open--)
                    {
                        if (!(result[open] is DelimiterNode o) || !o.CanOpen) continue;
                        if (o.Char != d.Char) continue;
                        if (o.Char != '~' && d.Char != '~') { }
                        foundOpen = o;
                        openIdx = open;
                        break;
                    }
                    if (foundOpen == null) continue;

                    int use = foundOpen.Length < d.Length ? foundOpen.Length : d.Length;
                    if (use == 0) continue;

                    var inner = new List<InlineNode>();
                    for (int k = openIdx + 1; k < close; k++) inner.Add(result[k]);
                    inner = ProcessDelimiters(inner);

                    InlineNode built = BuildEmphasis(foundOpen.Char, inner, use);

                    var newList = new List<InlineNode>();
                    for (int k = 0; k < openIdx; k++) newList.Add(result[k]);
                    if (foundOpen.Length - use > 0)
                        newList.Add(new TextNode(new string(foundOpen.Char, foundOpen.Length - use)));
                    newList.Add(built);
                    if (d.Length - use > 0)
                        newList.Add(new TextNode(new string(d.Char, d.Length - use)));
                    for (int k = close + 1; k < result.Count; k++) newList.Add(result[k]);

                    result = newList;
                    changed = true;
                    break;
                }
            }
            for (int i = 0; i < result.Count; i++)
            {
                if (result[i] is DelimiterNode leftover)
                    result[i] = new TextNode(new string(leftover.Char, leftover.Length));
            }
            return result;
        }

        private static InlineNode BuildEmphasis(char ch, List<InlineNode> inner, int use)
        {
            if (ch == '~')
            {
                var s = new StrikethroughNode();
                s.Children.AddRange(inner);
                return s;
            }
            if (use == 1)
            {
                var e = new EmphasisNode();
                e.Children.AddRange(inner);
                return e;
            }
            if (use == 2)
            {
                var s = new StrongNode();
                s.Children.AddRange(inner);
                return s;
            }
            if (use == 3)
            {
                var s = new StrongNode();
                s.Children.AddRange(inner);
                var e = new EmphasisNode();
                e.Children.Add(s);
                return e;
            }
            var st = new StrongNode();
            st.Children.AddRange(inner);
            return st;
        }
    }
}