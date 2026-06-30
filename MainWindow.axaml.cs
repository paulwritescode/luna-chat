using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using LunaChat.Services;
using LunaChat.ViewModels;

namespace LunaChat;

public partial class MainWindow : Window, IDialogService
{
    private readonly MainWindowViewModel _vm;
    private DispatcherTimer? _toastTimer;
    private double _zoom = 1.0;

    public MainWindow()
    {
        InitializeComponent();

        _vm = new MainWindowViewModel();
        DataContext = _vm;

        WireWindowControls();
        ApplyZoom();

        AddHandler(KeyDownEvent, OnGlobalKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);

        Opened += async (_, _) =>
        {
            try { await _vm.AttachAsync(this); }
            catch (Exception ex) { Toast($"Startup error: {ex.Message}"); }
        };
    }

    private void OnGlobalKeyDown(object? sender, KeyEventArgs e)
    {
        var mod = e.KeyModifiers.HasFlag(KeyModifiers.Meta) || e.KeyModifiers.HasFlag(KeyModifiers.Control);
        if (!mod) return;

        switch (e.Key)
        {
            case Key.OemPlus or Key.Add:
                _zoom = Math.Min(1.8, _zoom + 0.1);
                ApplyZoom();
                e.Handled = true;
                break;
            case Key.OemMinus or Key.Subtract:
                _zoom = Math.Max(0.7, _zoom - 0.1);
                ApplyZoom();
                e.Handled = true;
                break;
            case Key.D0 or Key.NumPad0:
                _zoom = 1.0;
                ApplyZoom();
                e.Handled = true;
                break;
        }
    }

    private void ApplyZoom()
    {
        var root = this.FindControl<LayoutTransformControl>("ZoomRoot");
        if (root != null)
            root.LayoutTransform = new Avalonia.Media.ScaleTransform(_zoom, _zoom);
    }

    private void WireWindowControls()
    {
        // Drag regions
        foreach (var name in new[] { "SidebarTop", "MainTop" })
        {
            var region = this.FindControl<Control>(name);
            if (region != null)
            {
                region.PointerPressed += (_, e) =>
                {
                    if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                        BeginMoveDrag(e);
                };
            }
        }

        // macOS shows native traffic lights at top-left; pad the sidebar toolbar
        // to clear them and hide our custom controls. Other platforms draw their own.
        var sidebarTop = this.FindControl<Grid>("SidebarTop");
        if (OperatingSystem.IsMacOS())
        {
            if (sidebarTop != null) sidebarTop.Margin = new Avalonia.Thickness(76, 0, 0, 0);
            var controls = this.FindControl<StackPanel>("WindowControls");
            if (controls != null) controls.IsVisible = false;
        }
        else
        {
            if (sidebarTop != null) sidebarTop.Margin = new Avalonia.Thickness(8, 0, 0, 0);
        }

        var min = this.FindControl<Button>("MinButton");
        if (min != null) min.Click += (_, _) => WindowState = WindowState.Minimized;

        var max = this.FindControl<Button>("MaxButton");
        if (max != null) max.Click += (_, _) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        var close = this.FindControl<Button>("CloseButton");
        if (close != null) close.Click += (_, _) => Close();
    }

    // ===== IDialogService =====

    public async Task<IReadOnlyList<string>> PickFilesAsync(string title, bool allowMultiple)
    {
        var result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = allowMultiple
        });

        return result
            .Select(f => f.TryGetLocalPath() ?? f.Path.LocalPath)
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();
    }

    public async Task<string?> PickFolderAsync(string title)
    {
        var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        var folder = result.FirstOrDefault();
        return folder?.TryGetLocalPath() ?? folder?.Path.LocalPath;
    }

    public async Task<bool> ConfirmAsync(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 380,
            Height = 170,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Avalonia.Media.SolidColorBrush.Parse("#141414"),
            CanResize = false
        };

        var tcs = new TaskCompletionSource<bool>();

        var ok = new Button { Content = "Confirm", Classes = { "danger" } };
        var cancel = new Button { Content = "Cancel", Classes = { "ghost" } };
        ok.Click += (_, _) => { tcs.TrySetResult(true); dialog.Close(); };
        cancel.Click += (_, _) => { tcs.TrySetResult(false); dialog.Close(); };

        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Foreground = Avalonia.Media.SolidColorBrush.Parse("#D4D4D4")
                },
                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Spacing = 10,
                    Children = { cancel, ok }
                }
            }
        };

        dialog.Closed += (_, _) => tcs.TrySetResult(false);
        await dialog.ShowDialog(this);
        return await tcs.Task;
    }

    public void Toast(string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var host = this.FindControl<Border>("ToastHost");
            var text = this.FindControl<TextBlock>("ToastText");
            if (host == null || text == null) return;

            text.Text = message;
            host.IsVisible = true;

            _toastTimer?.Stop();
            _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _toastTimer.Tick += (_, _) =>
            {
                host.IsVisible = false;
                _toastTimer?.Stop();
            };
            _toastTimer.Start();
        });
    }
}
