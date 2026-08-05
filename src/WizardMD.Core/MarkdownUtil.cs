using System.Collections.Generic;
using System.Text;

namespace WizardMD.Core
{
    internal static class MarkdownUtil
    {
        public static bool IsAsciiPunctuation(char c)
        {
            return (c >= 33 && c <= 47) || (c >= 58 && c <= 64) || (c >= 91 && c <= 96) || (c >= 123 && c <= 126);
        }

        public static bool IsWhitespace(char c)
        {
            return c == ' ' || c == '\t' || c == '\n' || c == '\r' || c == '\f';
        }

        public static bool IsAlnum(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
        }

        public static string NormalizeLabel(string label)
        {
            if (string.IsNullOrEmpty(label)) return "";
            var sb = new StringBuilder();
            bool lastSpace = false;
            foreach (char c in label.Trim().ToLowerInvariant())
            {
                if (IsWhitespace(c))
                {
                    if (!lastSpace) sb.Append(' ');
                    lastSpace = true;
                }
                else
                {
                    sb.Append(c);
                    lastSpace = false;
                }
            }
            return sb.ToString();
        }

        public static string Unescape(string s)
        {
            if (string.IsNullOrEmpty(s) || s.IndexOf('\\') < 0) return s;
            var sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\' && i + 1 < s.Length && IsAsciiPunctuation(s[i + 1]))
                {
                    sb.Append(s[i + 1]);
                    i++;
                }
                else
                {
                    sb.Append(s[i]);
                }
            }
            return sb.ToString();
        }

        private static readonly Dictionary<string, string> Entities = BuildEntities();

        private static Dictionary<string, string> BuildEntities()
        {
            var d = new Dictionary<string, string>
            {
                { "amp", "\u0026" },
                { "lt", "<" },
                { "gt", ">" },
                { "quot", "\"" },
                { "apos", "'" },
                { "nbsp", "\u00a0" },
                { "copy", "\u00a9" },
                { "reg", "\u00ae" },
                { "trade", "\u2122" },
                { "hellip", "\u2026" },
                { "mdash", "\u2014" },
                { "ndash", "\u2013" },
                { "ldquo", "\u201c" },
                { "rdquo", "\u201d" },
                { "lsquo", "\u2018" },
                { "rsquo", "\u2019" },
                { "laquo", "\u00ab" },
                { "raquo", "\u00bb" },
                { "times", "\u00d7" },
                { "divide", "\u00f7" },
                { "plusmn", "\u00b1" },
                { "middot", "\u00b7" },
                { "bull", "\u2022" },
                { "sect", "\u00a7" },
                { "para", "\u00b6" },
                { "deg", "\u00b0" },
                { "micro", "\u00b5" },
                { "euro", "\u20ac" },
                { "pound", "\u00a3" },
                { "yen", "\u00a5" },
                { "cent", "\u00a2" },
                { "alpha", "\u03b1" },
                { "beta", "\u03b2" },
                { "gamma", "\u03b3" },
                { "delta", "\u03b4" },
                { "pi", "\u03c0" },
                { "sum", "\u2211" },
                { "infin", "\u221e" },
            };
            return d;
        }

        public static bool TryEntity(string s, int ampIndex, out string value, out int end)
        {
            value = null;
            end = ampIndex;
            int semi = s.IndexOf(';', ampIndex + 1);
            if (semi < 0 || semi - ampIndex - 1 > 32) return false;
            string body = s.Substring(ampIndex + 1, semi - ampIndex - 1);
            if (body.Length == 0) return false;
            if (body[0] == '#')
            {
                int cp;
                try
                {
                    if (body.Length > 1 && (body[1] == 'x' || body[1] == 'X'))
                        cp = System.Convert.ToInt32(body.Substring(2), 16);
                    else
                        cp = System.Convert.ToInt32(body.Substring(1), 10);
                }
                catch
                {
                    return false;
                }
                if (cp < 0 || cp > 0x10FFFF) return false;
                value = new string((char)cp, 1);
                end = semi + 1;
                return true;
            }
            if (Entities.TryGetValue(body, out value))
            {
                end = semi + 1;
                return true;
            }
            return false;
        }
    }
}