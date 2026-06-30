using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using LunaChat.ViewModels;

namespace LunaChat.Views;

public partial class ChatView : UserControl
{
    private ChatViewModel? _vm;
    private double _lastRightWidth = 440;

    public ChatView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (_vm != null) _vm.ScrollToEndRequested -= ScrollToEnd;
        if (_vm != null) _vm.RightPanelChanged -= UpdateRightPanel;

        _vm = DataContext as ChatViewModel;

        if (_vm != null)
        {
            _vm.ScrollToEndRequested += ScrollToEnd;
            _vm.RightPanelChanged += UpdateRightPanel;
            UpdateRightPanel();
        }
    }

    private void UpdateRightPanel()
    {
        Dispatcher.UIThread.Post(() =>
        {
            var body = this.FindControl<Grid>("BodyGrid");
            if (body == null || _vm == null || body.ColumnDefinitions.Count < 3) return;
            var rightCol = body.ColumnDefinitions[2];

            if (_vm.ShowRightPanel)
            {
                if (rightCol.Width.Value <= 1)
                    rightCol.Width = new GridLength(_lastRightWidth);
                rightCol.MinWidth = 340;
                rightCol.MaxWidth = 860;
            }
            else
            {
                if (rightCol.Width.Value > 1)
                    _lastRightWidth = rightCol.Width.Value;
                rightCol.MinWidth = 0;
                rightCol.Width = new GridLength(0);
            }
        });
    }

    private void ScrollToEnd()
    {
        Dispatcher.UIThread.Post(() =>
        {
            var scroll = this.FindControl<ScrollViewer>("MessageScroll");
            scroll?.ScrollToEnd();
        }, DispatcherPriority.Background);
    }
}
