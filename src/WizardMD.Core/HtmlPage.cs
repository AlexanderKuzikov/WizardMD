using System.Text;

namespace WizardMD.Core;

/// <summary>
/// Строит полную HTML-страницу из markdown: темы light/dark, встроенная
/// подсветка синтаксиса (zero-dependency JS), стили документа.
/// </summary>
public static class HtmlPage
{
    private const string CssLight = """
        :root { color-scheme: light;
          --bg:#ffffff; --fg:#1f2328; --muted:#57606a; --link:#0969da;
          --code-bg:#f6f8fa; --border:#d0d7de; --quote-bg:#f6f8fa; --th-bg:#f6f8fa;
          --kwd:#cf222e; --str:#0a3069; --com:#6e7781; --num:#0550ae; --fn:#8250df; }
        """;

    private const string CssDark = """
        :root { color-scheme: dark;
          --bg:#0d1117; --fg:#e6edf3; --muted:#8b949e; --link:#58a6ff;
          --code-bg:#161b22; --border:#30363d; --quote-bg:#161b22; --th-bg:#161b22;
          --kwd:#ff7b72; --str:#a5d6ff; --com:#8b949e; --num:#79c0ff; --fn:#d2a8ff; }
        """;

    private const string CssBase = """
        * { box-sizing: border-box; }
        body { margin:0; background:var(--bg); color:var(--fg);
          font:15px/1.65 -apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,"Helvetica Neue",Arial,sans-serif; }
        main { max-width:880px; margin:0 auto; padding:32px 40px 96px; }
        h1,h2 { border-bottom:1px solid var(--border); padding-bottom:.3em; }
        h1 { font-size:1.9em; } h2 { font-size:1.45em; } h3 { font-size:1.2em; }
        a { color:var(--link); text-decoration:none; }
        a:hover { text-decoration:underline; }
        p { margin:.7em 0; }
        pre { background:var(--code-bg); padding:14px 16px; border-radius:6px; overflow:auto;
          font:13px/1.55 Consolas,"Cascadia Mono","Courier New",monospace; }
        code { font-family:Consolas,"Cascadia Mono","Courier New",monospace; font-size:88%;
          background:var(--code-bg); padding:.15em .4em; border-radius:4px; }
        pre code { background:none; padding:0; font-size:100%; }
        blockquote { border-left:4px solid var(--border); margin:1em 0; padding:.1em 1em;
          color:var(--muted); background:var(--quote-bg); border-radius:0 6px 6px 0; }
        table { border-collapse:collapse; margin:1em 0; }
        th,td { border:1px solid var(--border); padding:6px 13px; }
        th { background:var(--th-bg); }
        tr:nth-child(even) td { background:color-mix(in srgb, var(--th-bg) 55%, transparent); }
        hr { border:0; border-top:1px solid var(--border); margin:1.4em 0; }
        img { max-width:100%; }
        del { opacity:.65; }
        ul,ol { padding-left:2em; }
        li { margin:.2em 0; }
        li.task-list-item { list-style:none; margin-left:-1.5em; }
        li.task-list-item input { margin-right:.4em; }
        ::selection { background:var(--link); color:var(--bg); }
        """;

    private const string HighlighterJs = """
        (function(){
          var KW = {
            csharp: "abstract as base bool break byte case catch char checked class const continue decimal default delegate do double else enum event explicit extern false finally fixed float for foreach goto if implicit in int interface internal is lock long namespace new null object operator out override params private protected public readonly ref return sbyte sealed short sizeof stackalloc static string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using var virtual void volatile while async await dynamic record init required",
            python: "False None True and as assert async await break class continue def del elif else except finally for from global if import in is lambda nonlocal not or pass raise return try while with yield match case",
            javascript: "break case catch class const continue debugger default delete do else enum export extends false finally for function if import in instanceof new null return super switch this throw true try typeof var void while with yield let static async await",
            typescript: "break case catch class const continue debugger default delete do else enum export extends false finally for function if import in instanceof new null return super switch this throw true try typeof var void while with yield let static async await interface type implements readonly",
            go: "break case chan const continue default defer else fallthrough for func go goto if import interface map package range return select struct switch type var",
            java: "abstract assert boolean break byte case catch char class const continue default do double else enum extends final finally float for goto if implements import instanceof int interface long native new package private protected public return short static strictfp super switch synchronized this throw throws transient try void volatile while",
            cpp: "auto break case catch char class const continue default delete do double else enum explicit export extern false float for friend goto if inline int long mutable namespace new noexcept nullptr operator private protected public register reinterpret_cast return short signed sizeof static struct switch template this throw true try typedef typeid typename union unsigned using virtual void volatile wchar_t while",
            c: "auto break case char const continue default do double else enum extern float for goto if int long register restrict return short signed sizeof static struct switch typedef union unsigned void volatile while",
            ruby: "alias and begin break case class def defined do else elsif end ensure false for if in module next nil not or redo rescue retry return self super then true undef unless until when while yield",
            php: "abstract and array as break callable case catch class clone const continue declare default do echo else elseif empty enddeclare endfor endforeach endif endswitch endwhile extends final finally fn for foreach function global goto if implements include include_once instanceof insteadof interface isset list namespace new or print private protected public require require_once return static switch throw trait try unset use var while xor yield",
            sql: "select from where insert update delete create table index view drop alter add primary key foreign references join inner left right full outer on as and or not null is in between like order by group having limit offset union distinct exists case when then else end",
            bash: "case do done elif else esac fi for function if in select then time until while exit echo printf read set unset export local return break continue shift source",
            powershell: "begin break catch continue data do dynamicparam else elseif end exit filter finally for foreach from function if in param process return switch throw trap try until where while",
            json: "true false null",
            yaml: "true false null yes no on off",
            ini: "true false yes no on off",
            xml: "true false",
            html: "true false",
            css: "true false",
            ini_sec: ""
          };
          var COM = { csharp:1, javascript:1, typescript:1, java:1, cpp:1, c:1, go:1, css:1, php:1, ruby:1, sql:1 };
          var HASH = { python:1, bash:1, ruby:1, powershell:1, yaml:1, ini:1, php:1 };
          function esc(s){ return s.replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;"); }
          function getVar(n){ var v=getComputedStyle(document.documentElement).getPropertyValue(n).trim(); return v||"inherit"; }
          function rulesFor(lang){
            var r=[];
            if (COM[lang]) r.push({c:"--com", re:new RegExp("\\/\\*[\\s\\S]*?\\*\\/|\\/\\/[^\\n]*","g")});
            if (HASH[lang]) r.push({c:"--com", re:new RegExp("#[^\\n]*","g")});
            if (lang==="sql") r.push({c:"--com", re:new RegExp("--[^\\n]*|\\/\\*[\\s\\S]*?\\*\\/","g")});
            if (lang==="html"||lang==="xml") r.push({c:"--com", re:new RegExp("<!--[\\s\\S]*?-->","g")});
            r.push({c:"--str", re:new RegExp("\\\"(?:[^\\\"\\\\\\n]|\\\\.)*\\\"|'(?:[^'\\\\\\n]|\\\\.)*'|`(?:[^`\\\\]|\\\\.)*`","g")});
            r.push({c:"--num", re:new RegExp("\\b0[xX][0-9a-fA-F]+\\b|\\b\\d+(?:\\.\\d+)?(?:[eE][+-]?\\d+)?[fFdDmMlLuU]?\\b","g")});
            if (lang==="html"||lang==="xml") r.push({c:"--kwd", re:new RegExp("</?[a-zA-Z][^>\\n]*>","g")});
            if (lang==="css") r.push({c:"--fn", re:new RegExp("[a-zA-Z-]+(?=\\s*:)","g")});
            var kw=KW[lang]||"";
            if (kw) r.push({c:"--kwd", re:new RegExp("\\b(?:"+kw.replace(/ /g,"|")+")\\b","g")});
            r.push({c:"--fn", re:new RegExp("[A-Za-z_]\\w*(?=\\s*\\()","g")});
            return r;
          }
          function color(c){ return c.charAt(0)==="-" ? getVar(c) : c; }
          function tokenize(code, lang){
            var rules=rulesFor(lang), out="", pos=0;
            while(pos<code.length){
              var best=null, len=0;
              for(var i=0;i<rules.length;i++){
                rules[i].re.lastIndex=pos;
                var m=rules[i].re.exec(code);
                if(m && m.index===pos){ best=rules[i]; len=m[0].length; break; }
              }
              if(best){ out+='<span style="color:'+color(best.c)+'">'+esc(code.substr(pos,len))+'</span>'; pos+=len; }
              else { out+=esc(code.charAt(pos)); pos++; }
            }
            return out;
          }
          function highlight(){
            var blocks=document.querySelectorAll("pre code");
            for(var i=0;i<blocks.length;i++){
              var b=blocks[i], m=b.className.match(/language-([\w+-]+)/), lang=m?m[1].toLowerCase():null;
              if(!lang) continue;
              b.innerHTML=tokenize(b.textContent, lang);
            }
          }
          document.addEventListener("DOMContentLoaded", highlight);
          if(document.readyState!=="loading") highlight();
        })();
        """;

    public static string Build(string markdown, bool dark)
    {
        var css = dark ? CssDark : CssLight;
        var body = Markdown.ToHtml(markdown ?? "");
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html>\n<html lang=\"ru\">\n<head>\n<meta charset=\"utf-8\">\n");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
        sb.Append("<title>WizardMD</title>\n<style>\n").Append(css).Append('\n').Append(CssBase).Append("</style>\n</head>\n<body>\n<main>\n");
        sb.Append(body);
        sb.Append("\n</main>\n<script>\n").Append(HighlighterJs).Append("\n</script>\n</body>\n</html>\n");
        return sb.ToString();
    }
}