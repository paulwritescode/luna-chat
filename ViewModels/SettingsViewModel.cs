using System.Collections.ObjectModel;
using System.Diagnostics;
using LunaChat.Models;
using LunaChat.Services;

namespace LunaChat.ViewModels;

/// <summary>
/// Settings view: kiro config, folders, appearance, about.
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private readonly AppState _app;
    private readonly IDialogService _dialogs;

    public SettingsViewModel(AppState app, IDialogService dialogs)
    {
        _app = app;
        _dialogs = dialogs;

        var s = app.Settings;
        _kiroBinaryPath = s.KiroBinaryPath;
        _kiroConfigPath = s.KiroConfigPath;
        _skillsFolder = s.SkillsFolder;
        _inputFolder = s.DefaultInputFolder;
        _outputFolder = s.DefaultOutputFolder;
        _fontSize = s.FontSize;
        _monoFont = s.MonoFont;
        _theme = string.IsNullOrEmpty(s.Theme) ? "System" : s.Theme;

        TestConnectionCommand = new AsyncRelayCommand(_ => TestConnectionAsync());
        SaveCommand = new AsyncRelayCommand(_ => SaveAsync());
        BrowseBinaryCommand = new AsyncRelayCommand(_ => BrowseBinaryAsync());
        BrowseSkillsCommand = new AsyncRelayCommand(_ => BrowseFolderAsync(v => SkillsFolder = v));
        BrowseInputCommand = new AsyncRelayCommand(_ => BrowseFolderAsync(v => InputFolder = v));
        BrowseOutputCommand = new AsyncRelayCommand(_ => BrowseFolderAsync(v => OutputFolder = v));
        CheckUpdatesCommand = new RelayCommand(_ => OpenReleases());
    }

    private string _kiroBinaryPath;
    public string KiroBinaryPath { get => _kiroBinaryPath; set => SetField(ref _kiroBinaryPath, value); }

    private string _kiroConfigPath;
    public string KiroConfigPath { get => _kiroConfigPath; set => SetField(ref _kiroConfigPath, value); }

    private string _skillsFolder;
    public string SkillsFolder { get => _skillsFolder; set => SetField(ref _skillsFolder, value); }

    private string _inputFolder;
    public string InputFolder { get => _inputFolder; set => SetField(ref _inputFolder, value); }

    private string _outputFolder;
    public string OutputFolder { get => _outputFolder; set => SetField(ref _outputFolder, value); }

    private int _fontSize;
    public int FontSize { get => _fontSize; set => SetField(ref _fontSize, value); }

    public ObservableCollection<int> FontSizeOptions { get; } = new() { 12, 13, 14 };

    private string _monoFont;
    public string MonoFont { get => _monoFont; set => SetField(ref _monoFont, value); }

    public ObservableCollection<string> MonoFontOptions { get; } = new()
    {
        "JetBrains Mono", "Fira Code", "Cascadia Code"
    };

    private string _theme;
    public string Theme
    {
        get => _theme;
        set
        {
            if (SetField(ref _theme, value))
                ApplyTheme(value);
        }
    }

    public ObservableCollection<string> ThemeOptions { get; } = new() { "System", "Light", "Dark" };

    private void ApplyTheme(string theme)
    {
        var variant = theme switch
        {
            "Light" => Avalonia.Styling.ThemeVariant.Light,
            "Dark" => Avalonia.Styling.ThemeVariant.Dark,
            _ => Avalonia.Styling.ThemeVariant.Default
        };
        if (Avalonia.Application.Current != null)
            Avalonia.Application.Current.RequestedThemeVariant = variant;
        _app.Settings.Theme = theme;
    }

    private string _testResult = "";
    public string TestResult { get => _testResult; set => SetField(ref _testResult, value); }

    private bool _testSuccess;
    public bool TestSuccess { get => _testSuccess; set => SetField(ref _testSuccess, value); }

    public string AppVersion => typeof(SettingsViewModel).Assembly.GetName().Version?.ToString() ?? "1.0.0";
    public string KiroVersionText => string.IsNullOrEmpty(_app.KiroVersion) ? "unknown" : _app.KiroVersion;

    public AsyncRelayCommand TestConnectionCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public AsyncRelayCommand BrowseBinaryCommand { get; }
    public AsyncRelayCommand BrowseSkillsCommand { get; }
    public AsyncRelayCommand BrowseInputCommand { get; }
    public AsyncRelayCommand BrowseOutputCommand { get; }
    public RelayCommand CheckUpdatesCommand { get; }

    private async Task TestConnectionAsync()
    {
        var binary = KiroLocator.FindKiroBinary(KiroBinaryPath);
        if (binary == null)
        {
            TestSuccess = false;
            TestResult = "kiro binary not found";
            return;
        }

        var runner = new KiroRunner(binary);
        var version = await runner.GetVersionAsync();
        if (version == null)
        {
            TestSuccess = false;
            TestResult = $"found at {binary} but --version failed";
        }
        else
        {
            TestSuccess = true;
            TestResult = $"{binary}\n{version}";
        }
    }

    private async Task SaveAsync()
    {
        var s = _app.Settings;
        s.KiroBinaryPath = KiroBinaryPath;
        s.KiroConfigPath = KiroConfigPath;
        s.SkillsFolder = SkillsFolder;
        s.DefaultInputFolder = InputFolder;
        s.DefaultOutputFolder = OutputFolder;
        s.FontSize = FontSize;
        s.MonoFont = MonoFont;
        s.Theme = Theme;

        await _app.SettingsStore.SaveAsync(s);
        await _app.ReloadSkillsAsync();
        await _app.RefreshKiroStatusAsync();
        _dialogs.Toast("Settings saved");
    }

    private async Task BrowseBinaryAsync()
    {
        var files = await _dialogs.PickFilesAsync("Select kiro binary", allowMultiple: false);
        if (files.Count > 0) KiroBinaryPath = files[0];
    }

    private async Task BrowseFolderAsync(Action<string> assign)
    {
        var folder = await _dialogs.PickFolderAsync("Select folder");
        if (folder != null) assign(folder);
    }

    private void OpenReleases()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/",
                UseShellExecute = true
            });
        }
        catch (Exception ex) { Console.Error.WriteLine(ex.Message); }
    }
}
