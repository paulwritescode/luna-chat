using Avalonia;
using Avalonia.Controls;

namespace LunaChat.Controls;

/// <summary>
/// Renders a Markdown string into themed Avalonia controls.
/// Re-renders on content, font-size, or theme changes.
/// </summary>
public class MarkdownBlock : Decorator
{
    public static readonly StyledProperty<string?> MarkdownProperty =
        AvaloniaProperty.Register<MarkdownBlock, string?>(nameof(Markdown));

    public static readonly StyledProperty<double> BodyFontSizeProperty =
        AvaloniaProperty.Register<MarkdownBlock, double>(nameof(BodyFontSize), 14d);

    public string? Markdown
    {
        get => GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    public double BodyFontSize
    {
        get => GetValue(BodyFontSizeProperty);
        set => SetValue(BodyFontSizeProperty, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ActualThemeVariantChanged += OnThemeChanged;
        Rebuild();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        ActualThemeVariantChanged -= OnThemeChanged;
    }

    private void OnThemeChanged(object? sender, System.EventArgs e) => Rebuild();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MarkdownProperty || change.Property == BodyFontSizeProperty)
            Rebuild();
    }

    private void Rebuild()
    {
        Child = MarkdownRenderer.Render(Markdown ?? "", BodyFontSize, this);
    }
}
