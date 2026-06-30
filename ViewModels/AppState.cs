using System.Collections.ObjectModel;
using System.Threading;
using LunaChat.Models;
using LunaChat.Services;

namespace LunaChat.ViewModels;

/// <summary>
/// Root runtime state and service container. Not persisted directly.
/// </summary>
public class AppState : ViewModelBase
{
    public AppState()
    {
        SettingsStore = new AppSettingsStore();
        SessionStore = new SessionStore();
        SkillLoader = new SkillLoader();
        Settings = SettingsStore.Load();
        PromptBuilder = new PromptBuilder(id => Skills.FirstOrDefault(s => s.Id == id));
    }

    // Services
    public AppSettingsStore SettingsStore { get; }
    public SessionStore SessionStore { get; }
    public SkillLoader SkillLoader { get; }
    public PromptBuilder PromptBuilder { get; }

    // Settings
    public AppSettings Settings { get; private set; }

    public void ReplaceSettings(AppSettings settings) => Settings = settings;

    // Loaded skills
    public ObservableCollection<SkillDefinition> Skills { get; } = new();

    // All sessions, sorted by UpdatedAt desc
    public ObservableCollection<Session> Sessions { get; } = new();

    private Session? _activeSession;
    public Session? ActiveSession
    {
        get => _activeSession;
        set => SetField(ref _activeSession, value);
    }

    private NavRoute _currentRoute = NavRoute.Chat;
    public NavRoute CurrentRoute
    {
        get => _currentRoute;
        set => SetField(ref _currentRoute, value);
    }

    private KiroStatus _kiroStatus = KiroStatus.Unknown;
    public KiroStatus KiroStatus
    {
        get => _kiroStatus;
        set
        {
            if (SetField(ref _kiroStatus, value))
            {
                OnPropertyChanged(nameof(KiroStatusText));
                OnPropertyChanged(nameof(IsKiroReady));
            }
        }
    }

    public bool IsKiroReady => KiroStatus == KiroStatus.Ready;

    public string KiroStatusText => KiroStatus switch
    {
        KiroStatus.Ready => "kiro ready",
        KiroStatus.NotFound => "kiro not found",
        KiroStatus.Error => "kiro error",
        _ => "checking kiro…"
    };

    public string KiroVersion { get; set; } = "";

    private string _kiroBinaryPath = "";
    public string KiroBinaryPath
    {
        get => _kiroBinaryPath;
        set => SetField(ref _kiroBinaryPath, value);
    }

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set => SetField(ref _isRunning, value);
    }

    public CancellationTokenSource? RunCts { get; set; }

    // Composer attachments shared across views (File Browser can add to it)
    public ObservableCollection<AttachmentViewModel> ComposerAttachments { get; } = new();

    /// <summary>Add a file attachment (max 3, no duplicates). Returns false if rejected.</summary>
    public bool AddAttachment(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (ComposerAttachments.Count >= 3) return false;
        if (ComposerAttachments.Any(a => string.Equals(a.FullPath, path, StringComparison.OrdinalIgnoreCase)))
            return false;
        ComposerAttachments.Add(new AttachmentViewModel(path, a => ComposerAttachments.Remove(a)));
        return true;
    }

    /// <summary>Resolve effective input folder for the active session.</summary>
    public string InputFolderPath =>
        ActiveSession?.InputFolder ?? Settings.DefaultInputFolder;

    /// <summary>Resolve effective output folder for the active session.</summary>
    public string OutputFolderPath =>
        ActiveSession?.OutputFolder ?? Settings.DefaultOutputFolder;

    /// <summary>Load skills from the configured folder.</summary>
    public async Task ReloadSkillsAsync()
    {
        Skills.Clear();
        var loaded = await SkillLoader.LoadAllAsync(Settings.SkillsFolder);
        foreach (var s in loaded) Skills.Add(s);
    }

    /// <summary>Check kiro availability and update status.</summary>
    public async Task RefreshKiroStatusAsync()
    {
        var binary = KiroLocator.FindKiroBinary(Settings.KiroBinaryPath);
        if (binary == null)
        {
            KiroBinaryPath = "";
            KiroVersion = "";
            KiroStatus = KiroStatus.NotFound;
            return;
        }

        KiroBinaryPath = binary;
        var runner = new KiroRunner(binary);
        var version = await runner.GetVersionAsync();
        if (version == null)
        {
            KiroStatus = KiroStatus.Error;
        }
        else
        {
            KiroVersion = version;
            KiroStatus = KiroStatus.Ready;
        }
    }
}
