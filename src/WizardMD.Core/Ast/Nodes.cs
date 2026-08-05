using System.Collections.Generic;

namespace WizardMD.Core.Ast
{
    public abstract class Node
    {
    }

    public sealed class Document : Node
    {
        public List<Node> Blocks { get; } = new List<Node>();
    }

    // ---------- Block nodes ----------

    public sealed class ParagraphBlock : Node
    {
        public List<string> RawLines { get; } = new List<string>();
        public List<InlineNode> Inlines { get; } = new List<InlineNode>();
    }

    public sealed class HeadingBlock : Node
    {
        public HeadingBlock(int level)
        {
            Level = level;
        }

        public int Level { get; }
        public List<InlineNode> Inlines { get; } = new List<InlineNode>();
    }

    public sealed class ListBlock : Node
    {
        public bool IsOrdered { get; set; }
        public char Bullet { get; set; } = '-';
        public int Start { get; set; } = 1;
        public bool IsLoose { get; set; }
        public List<ListItemBlock> Items { get; } = new List<ListItemBlock>();
    }

    public sealed class ListItemBlock : Node
    {
        public bool IsTask { get; set; }
        public bool TaskChecked { get; set; }
        public List<Node> Blocks { get; } = new List<Node>();
    }

    public sealed class BlockQuoteBlock : Node
    {
        public List<Node> Blocks { get; } = new List<Node>();
    }

    public sealed class CodeBlock : Node
    {
        public bool IsFenced { get; set; }
        public string Info { get; set; } = "";
        public string Text { get; set; } = "";
    }

    public sealed class ThematicBreakBlock : Node
    {
    }

    public enum TableAlign
    {
        None,
        Left,
        Center,
        Right
    }

    public sealed class TableBlock : Node
    {
        public List<TableAlign> Aligns { get; } = new List<TableAlign>();
        public List<TableRow> Rows { get; } = new List<TableRow>();
        public bool HasHeader { get; set; }
    }

    public sealed class TableRow : Node
    {
        public List<List<InlineNode>> Cells { get; } = new List<List<InlineNode>>();
    }

    // ---------- Inline nodes ----------

    public abstract class InlineNode : Node
    {
    }

    public sealed class TextNode : InlineNode
    {
        public TextNode(string text)
        {
            Text = text;
        }

        public string Text { get; }
    }

    public sealed class StrongNode : InlineNode
    {
        public List<InlineNode> Children { get; } = new List<InlineNode>();
    }

    public sealed class EmphasisNode : InlineNode
    {
        public List<InlineNode> Children { get; } = new List<InlineNode>();
    }

    public sealed class StrikethroughNode : InlineNode
    {
        public List<InlineNode> Children { get; } = new List<InlineNode>();
    }

    public sealed class CodeNode : InlineNode
    {
        public CodeNode(string text)
        {
            Text = text;
        }

        public string Text { get; }
    }

    public sealed class LinkNode : InlineNode
    {
        public string Url { get; set; } = "";
        public string Title { get; set; } = "";
        public List<InlineNode> Children { get; } = new List<InlineNode>();
    }

    public sealed class ImageNode : InlineNode
    {
        public string Url { get; set; } = "";
        public string Title { get; set; } = "";
        public List<InlineNode> Children { get; } = new List<InlineNode>();
    }

    public sealed class AutoLinkNode : InlineNode
    {
        public string Url { get; set; } = "";
        public string Label { get; set; } = "";
    }

    public sealed class SoftBreakNode : InlineNode
    {
    }

    public sealed class HardBreakNode : InlineNode
    {
    }
}