using System.Collections.ObjectModel;
using LunaChat.Models;
using LunaChat.Services;

namespace LunaChat.ViewModels;

public class SessionListItemViewModel : ViewModelBase
{
    public SessionListItemViewModel(Session session, bool isActive)
    {
        Session = session;
        _isActive = isActive;
    }

    public Session Session { get; }
    public string Name => Session.Name;

    private bool _isActive;
    public bool IsActive { get => _isActive; set => SetField(ref _isActive, value); }

    public string DateBadge
    {
        get
        {
            var local = Session.UpdatedAt.ToLocalTime().Date;
            var today = DateTime.Now.Date;
            if (local == today) return "Today";
            if (local == today.AddDays(-1)) return "Yesterday";
            return Session.UpdatedAt.ToLocalTime().ToString("dd MMM");
        }
    }

    public string SkillCountText
    {
        get
        {
            var n = Session.ActiveSkillIds.Count;
            return n == 1 ? "1 skill" : $"{n} skills";
        }
    }
}

/// <summary>
/// Root window view model: nav, sessions, kiro status, sub-views.
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    private IDialogService? _dialogs;

    public MainWindowViewModel()
    {
        App = new AppState();
        // Sub-view models are created once dialogs are wired in Attach().
    }

    public AppState App { get; }

    public ChatViewModel Chat { get; private set; } = null!;
    public FileBrowserViewModel FileBrowser { get; private set; } = null!;
    public SettingsViewModel Settings { get; private set; } = null!;

    public ObservableCollection<SessionListItemViewModel> SessionItems { get; } = new();

    // ----- Session search -----
    private bool _isSearchVisible;
    public bool IsSearchVisible
    {
        get => _isSearchVisible;
        set => SetField(ref _isSearchVisible, value);
    }

    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetField(ref _searchText, value))
                RefreshSessionList();
        }
    }

    public RelayCommand ToggleSearchCommand => new(_ =>
    {
        IsSearchVisible = !IsSearchVisible;
        if (!IsSearchVisible) SearchText = "";
    });

    // ----- Sidebar collapse -----
    private bool _isSidebarCollapsed;
    public bool IsSidebarCollapsed
    {
        get => _isSidebarCollapsed;
        set
        {
            if (SetField(ref _isSidebarCollapsed, value))
                OnPropertyChanged(nameof(IsSidebarVisible));
        }
    }

    public bool IsSidebarVisible => !_isSidebarCollapsed;

    public RelayCommand ToggleSidebarCommand => new(_ => IsSidebarCollapsed = !IsSidebarCollapsed);

    // ----- Theme -----
    private bool _isDarkTheme;
    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set => SetField(ref _isDarkTheme, value);
    }

    public RelayCommand ToggleThemeCommand => new(_ => SetTheme(IsDarkTheme ? "Light" : "Dark"));

    public void SetTheme(string theme)
    {
        var variant = theme switch
        {
            "Light" => Avalonia.Styling.ThemeVariant.Light,
            "Dark" => Avalonia.Styling.ThemeVariant.Dark,
            _ => Avalonia.Styling.ThemeVariant.Default
        };

        if (Avalonia.Application.Current != null)
            Avalonia.Application.Current.RequestedThemeVariant = variant;

        IsDarkTheme = ResolveIsDark(variant);

        App.Settings.Theme = theme;
        _ = App.SettingsStore.SaveAsync(App.Settings);
    }

    private static bool ResolveIsDark(Avalonia.Styling.ThemeVariant variant)
    {
        if (variant == Avalonia.Styling.ThemeVariant.Dark) return true;
        if (variant == Avalonia.Styling.ThemeVariant.Light) return false;
        var actual = Avalonia.Application.Current?.ActualThemeVariant;
        return actual == Avalonia.Styling.ThemeVariant.Dark;
    }

    // Nav state
    public bool IsChatRoute => App.CurrentRoute == NavRoute.Chat;
    public bool IsFilesRoute => App.CurrentRoute == NavRoute.Files;
    public bool IsSettingsRoute => App.CurrentRoute == NavRoute.Settings;

    public RelayCommand NavChatCommand => new(_ => Navigate(NavRoute.Chat));
    public RelayCommand NavFilesCommand => new(_ => Navigate(NavRoute.Files));
    public RelayCommand NavSettingsCommand => new(_ => Navigate(NavRoute.Settings));
    public RelayCommand NewSessionCommand => new(_ => NewSession());

    public RelayCommand SelectSessionCommand => new(p =>
    {
        if (p is SessionListItemViewModel item)
        {
            App.ActiveSession = item.Session;
            Navigate(NavRoute.Chat);
        }
    });

    // kiro status popover
    private bool _kiroPopoverOpen;
    public bool KiroPopoverOpen { get => _kiroPopoverOpen; set => SetField(ref _kiroPopoverOpen, value); }
    public RelayCommand ToggleKiroPopoverCommand => new(_ => KiroPopoverOpen = !KiroPopoverOpen);
    public RelayCommand ReconfigureKiroCommand => new(_ =>
    {
        KiroPopoverOpen = false;
        Navigate(NavRoute.Settings);
    });

    /// <summary>Called by the window once an IDialogService is available.</summary>
    public async Task AttachAsync(IDialogService dialogs)
    {
        _dialogs = dialogs;
        Chat = new ChatViewModel(App, dialogs);
        FileBrowser = new FileBrowserViewModel(App, dialogs);
        Settings = new SettingsViewModel(App, dialogs);
        OnPropertyChanged(nameof(Chat));
        OnPropertyChanged(nameof(FileBrowser));
        OnPropertyChanged(nameof(Settings));

        SetTheme(App.Settings.Theme);

        App.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AppState.CurrentRoute))
            {
                OnPropertyChanged(nameof(IsChatRoute));
                OnPropertyChanged(nameof(IsFilesRoute));
                OnPropertyChanged(nameof(IsSettingsRoute));
            }
            if (e.PropertyName is nameof(AppState.Sessions) or nameof(AppState.ActiveSession))
                RefreshSessionList();
        };

        await App.RefreshKiroStatusAsync();
        await App.ReloadSkillsAsync();
        await LoadSessionsAsync();
        Chat.RebuildSkillRail();
    }

    private async Task LoadSessionsAsync()
    {
        var sessions = await App.SessionStore.LoadAllAsync();
        App.Sessions.Clear();
        foreach (var s in sessions) App.Sessions.Add(s);

        if (App.Sessions.Count == 0)
            NewSession();
        else
            App.ActiveSession = App.Sessions[0];

        RefreshSessionList();
    }

    private void RefreshSessionList()
    {
        SessionItems.Clear();
        var query = App.Sessions.OrderByDescending(x => x.UpdatedAt).AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
            query = query.Where(s => s.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        foreach (var s in query.Take(50))
            SessionItems.Add(new SessionListItemViewModel(s, ReferenceEquals(s, App.ActiveSession)));
    }

    private void NewSession()
    {
        var session = new Session
        {
            InputFolder = null,
            OutputFolder = null
        };
        App.Sessions.Insert(0, session);
        App.ActiveSession = session;
        RefreshSessionList();
        Navigate(NavRoute.Chat);
    }

    private void Navigate(NavRoute route)
    {
        App.CurrentRoute = route;
        if (route == NavRoute.Files) FileBrowser?.Refresh();
    }
}
