using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using LunaChat.ViewModels;

namespace LunaChat.Views;

public partial class ComposerCard : UserControl
{
    public ComposerCard()
    {
        InitializeComponent();
    }

    private void OnCardPressed(object? sender, PointerPressedEventArgs e)
    {
        // Clicking anywhere on the card focuses the input, so the whole card
        // behaves as the text field. Ignore clicks on interactive children.
        if (e.Source is Visual v && v.FindAncestorOfType<Button>() != null)
            return;
        if (e.Source is Visual v2 && v2.FindAncestorOfType<ToggleButton>() != null)
            return;

        var box = this.FindControl<TextBox>("ComposerBox");
        box?.Focus();
    }

    private void OnComposerKeyDown(object? sender, KeyEventArgs e)
    {
        var isModifier = e.KeyModifiers.HasFlag(KeyModifiers.Meta) ||
                         e.KeyModifiers.HasFlag(KeyModifiers.Control);
        if (e.Key == Key.Enter && isModifier)
        {
            e.Handled = true;
            if (DataContext is ChatViewModel vm && vm.PrimaryActionCommand.CanExecute(null))
                vm.PrimaryActionCommand.Execute(null);
        }
    }
}
