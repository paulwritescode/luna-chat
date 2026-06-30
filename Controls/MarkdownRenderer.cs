using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MdInline = Markdig.Syntax.Inlines.Inline;

namespace LunaChat.Controls;

/// <summary>
/// Renders a subset of Markdown into themed Avalonia controls matching the
/// Codex conversation style (inline code chips, bullets, links, code blocks, tables).
/// Colors are resolved from the current theme via the supplied resource host.
/// </summary>
public static class MarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    private const string Mono = "JetBrains Mono,Cascadia Code,Menlo,Consolas,monospace";

    private sealed class Palette
    {
        public IBrush TextPrimary = Brushes.Black;
        public IBrush TextSecondary = Brushes.Gray;
        public IBrush Accent = Brushes.MediumPurple;
        public IBrush CodeBg = Brushes.Gainsboro;
        public IBrush CodeText = Brushes.MediumPurple;
        public IBrush BorderSubtle = Brushes.LightGray;
    }

    private static IBrush Resolve(Control host, string key, IBrush fallback)
    {
        if (host.TryFindResource(key, host.ActualThemeVariant, out var v) && v is IBrush b)
            return b;
        return fallback;
    }

    public static Control Render(string markdown, double fontSize, Control host)
    {
        var p = new Palette
        {
            TextPrimary = Resolve(host, "TextPrimary", Brushes.Black),
            TextSecondary = Resolve(host, "TextSecondary", Brushes.Gray),
            Accent = Resolve(host, "Accent", Brushes.MediumPurple),
            CodeBg = Resolve(host, "CodeBg", Brushes.Gainsboro),
            CodeText = Resolve(host, "CodeText", Brushes.MediumPurple),
            BorderSubtle = Resolve(host, "BorderSubtle", Brushes.LightGray)
        };

        var root = new StackPanel { Spacing = 10 };
        if (string.IsNullOrEmpty(markdown))
            return root;

        MarkdownDocument doc;
        try { doc = Markdown.Parse(markdown, Pipeline); }
        catch
        {
            root.Children.Add(BodyText(markdown, fontSize, p));
            return root;
        }

        foreach (var block in doc)
            RenderBlock(block, root, fontSize, p);

        return root;
    }

    private static void RenderBlock(Block block, Panel parent, double fontSize, Palette p)
    {
        switch (block)
        {
            case HeadingBlock heading:
                parent.Children.Add(new TextBlock
                {
                    Text = GetInlineText(heading.Inline),
                    Foreground = p.TextPrimary,
                    FontWeight = FontWeight.SemiBold,
                    FontSize = fontSize + (heading.Level <= 1 ? 5 : 2),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 6, 0, 0)
                });
                break;

            case FencedCodeBlock or CodeBlock:
                var code = GetCodeText((LeafBlock)block);
                parent.Children.Add(new Border
                {
                    Background = p.CodeBg,
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(14, 12),
                    Child = new SelectableTextBlock
                    {
                        Text = code,
                        FontFamily = new FontFamily(Mono),
                        FontSize = fontSize - 0.5,
                        Foreground = p.TextPrimary,
                        TextWrapping = TextWrapping.Wrap
                    }
                });
                break;

            case ListBlock list:
                var listPanel = new StackPanel { Spacing = 6 };
                foreach (var item in list)
                {
                    if (item is not ListItemBlock li) continue;
                    foreach (var sub in li)
                    {
                        if (sub is ParagraphBlock pb)
                        {
                            var row = new Grid
                            {
                                ColumnDefinitions = new ColumnDefinitions("16,*"),
                                Margin = new Thickness(2, 0, 0, 0)
                            };
                            var bullet = new TextBlock
                            {
                                Text = "•",
                                Foreground = p.TextSecondary,
                                FontSize = fontSize,
                                VerticalAlignment = VerticalAlignment.Top
                            };
                            Grid.SetColumn(bullet, 0);
                            var content = new SelectableTextBlock
                            {
                                Inlines = BuildInlines(pb.Inline, fontSize, p),
                                TextWrapping = TextWrapping.Wrap,
                                Foreground = p.TextPrimary,
                                FontSize = fontSize,
                                LineHeight = fontSize * 1.55
                            };
                            Grid.SetColumn(content, 1);
                            row.Children.Add(bullet);
                            row.Children.Add(content);
                            listPanel.Children.Add(row);
                        }
                        else RenderBlock(sub, listPanel, fontSize, p);
                    }
                }
                parent.Children.Add(listPanel);
                break;

            case Markdig.Extensions.Tables.Table table:
                parent.Children.Add(RenderTable(table, fontSize, p));
                break;

            case ParagraphBlock para:
                parent.Children.Add(new SelectableTextBlock
                {
                    Inlines = BuildInlines(para.Inline, fontSize, p),
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = p.TextPrimary,
                    FontSize = fontSize,
                    LineHeight = fontSize * 1.6
                });
                break;

            case ContainerBlock container:
                foreach (var child in container)
                    RenderBlock(child, parent, fontSize, p);
                break;
        }
    }

    private static Control RenderTable(Markdig.Extensions.Tables.Table table, double fontSize, Palette p)
    {
        var grid = new Grid();
        int rowIndex = 0;
        bool colsBuilt = false;

        foreach (var rowObj in table)
        {
            if (rowObj is not Markdig.Extensions.Tables.TableRow row) continue;
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            int colIndex = 0;
            foreach (var cellObj in row)
            {
                if (cellObj is not Markdig.Extensions.Tables.TableCell cell) continue;
                if (!colsBuilt && rowIndex == 0)
                    grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

                var text = "";
                foreach (var b in cell)
                    if (b is LeafBlock lb) text += GetInlineText(lb.Inline);

                var border = new Border
                {
                    BorderBrush = p.BorderSubtle,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(10, 6),
                    Child = new TextBlock
                    {
                        Text = text,
                        Foreground = p.TextPrimary,
                        FontWeight = row.IsHeader ? FontWeight.SemiBold : FontWeight.Normal,
                        FontSize = fontSize - 1,
                        TextWrapping = TextWrapping.Wrap
                    }
                };
                Grid.SetRow(border, rowIndex);
                Grid.SetColumn(border, colIndex);
                grid.Children.Add(border);
                colIndex++;
            }
            colsBuilt = true;
            rowIndex++;
        }

        return new Border { Child = grid };
    }

    private static InlineCollection BuildInlines(ContainerInline? container, double fontSize, Palette p)
    {
        var inlines = new InlineCollection();
        if (container == null) return inlines;

        foreach (var inline in container)
        {
            switch (inline)
            {
                case LiteralInline lit:
                    inlines.Add(new Run(lit.Content.ToString()) { Foreground = p.TextPrimary });
                    break;
                case EmphasisInline em:
                    var isBold = em.DelimiterCount == 2;
                    inlines.Add(new Run(GetInlineText(em))
                    {
                        Foreground = p.TextPrimary,
                        FontWeight = isBold ? FontWeight.Bold : FontWeight.Normal,
                        FontStyle = isBold ? FontStyle.Normal : FontStyle.Italic
                    });
                    break;
                case CodeInline code:
                    inlines.Add(new InlineUIContainer(new Border
                    {
                        Background = p.CodeBg,
                        CornerRadius = new CornerRadius(5),
                        Padding = new Thickness(5, 1),
                        Margin = new Thickness(1, 0),
                        Child = new TextBlock
                        {
                            Text = code.Content,
                            FontFamily = new FontFamily(Mono),
                            FontSize = fontSize - 1,
                            Foreground = p.CodeText
                        }
                    }));
                    break;
                case LineBreakInline:
                    inlines.Add(new LineBreak());
                    break;
                case LinkInline link:
                    inlines.Add(new Run(GetInlineText(link))
                    {
                        Foreground = p.Accent,
                        TextDecorations = TextDecorations.Underline
                    });
                    break;
                default:
                    var t = GetInlineText(inline as ContainerInline);
                    if (!string.IsNullOrEmpty(t))
                        inlines.Add(new Run(t) { Foreground = p.TextPrimary });
                    break;
            }
        }
        return inlines;
    }

    private static string GetInlineText(MdInline? inline)
    {
        switch (inline)
        {
            case null: return "";
            case LiteralInline lit: return lit.Content.ToString();
            case CodeInline code: return code.Content;
            case LineBreakInline: return " ";
            case ContainerInline container:
                var sb = new System.Text.StringBuilder();
                foreach (var child in container)
                    sb.Append(GetInlineText(child));
                return sb.ToString();
            default: return "";
        }
    }

    private static string GetCodeText(LeafBlock block)
    {
        if (block.Lines.Lines == null) return "";
        var sb = new System.Text.StringBuilder();
        var lines = block.Lines;
        for (int i = 0; i < lines.Count; i++)
            sb.AppendLine(lines.Lines[i].Slice.ToString());
        return sb.ToString().TrimEnd();
    }

    private static TextBlock BodyText(string text, double fontSize, Palette p) => new()
    {
        Text = text,
        Foreground = p.TextPrimary,
        FontSize = fontSize,
        TextWrapping = TextWrapping.Wrap
    };
}
