using Avalonia.Controls;
using Avalonia.Input;
using LunaChat.ViewModels;

namespace LunaChat.Views;

public partial class ComposerCard : UserControl
{
    public ComposerCard()
    {
        InitializeComponent();
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
