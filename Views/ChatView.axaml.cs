using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using LunaChat.ViewModels;

namespace LunaChat.Views;

public partial class ChatView : UserControl
{
    private ChatViewModel? _vm;
    private double _lastPreviewWidth = 460;

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

            if (!_vm.ShowRightPanel)
            {
                rightCol.MinWidth = 0;
                rightCol.MaxWidth = double.PositiveInfinity;
                rightCol.Width = new GridLength(0);
            }
            else if (_vm.IsPreviewOpen)
            {
                rightCol.MinWidth = 360;
                rightCol.MaxWidth = 900;
                rightCol.Width = new GridLength(_lastPreviewWidth);
            }
            else
            {
                // session card: fixed, compact
                rightCol.MinWidth = 240;
                rightCol.MaxWidth = 360;
                rightCol.Width = new GridLength(272);
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
