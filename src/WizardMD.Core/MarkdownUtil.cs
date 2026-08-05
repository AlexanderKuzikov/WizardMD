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

        private static readonly Dictionary<string, string> Entities = HtmlEntities.Map;

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

        public static string DecodeEntities(string s)
        {
            if (string.IsNullOrEmpty(s) || s.IndexOf('&') < 0) return s;
            var sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '&' && TryEntity(s, i, out string v, out int end))
                {
                    sb.Append(v);
                    i = end - 1;
                }
                else
                {
                    sb.Append(s[i]);
                }
            }
            return sb.ToString();
        }

        public static string NormalizeUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return "";
            string s = DecodeEntities(url);
            var sb = new StringBuilder(s.Length + 8);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')
                    || c == '-' || c == '.' || c == '_' || c == '~'
                    || c == '!' || c == '$' || c == '&' || c == '\'' || c == '(' || c == ')'
                    || c == '*' || c == '+' || c == ',' || c == ';' || c == '=' || c == ':'
                    || c == '@' || c == '/' || c == '?' || c == '#')
                {
                    sb.Append(c);
                }
                else if (c == '%' && i + 2 < s.Length
                         && IsHex(s[i + 1]) && IsHex(s[i + 2]))
                {
                    sb.Append(c).Append(s[i + 1]).Append(s[i + 2]);
                    i += 2;
                }
                else
                {
                    foreach (byte b in System.Text.Encoding.UTF8.GetBytes(c.ToString()))
                    {
                        sb.Append('%').Append(b.ToString("X2"));
                    }
                }
            }
            return sb.ToString();
        }

        private static bool IsHex(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
        }
    }
}