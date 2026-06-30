using System.Collections.ObjectModel;
using System.Threading;
using Avalonia.Threading;
using LunaChat.Models;
using LunaChat.Services;

namespace LunaChat.ViewModels;

/// <summary>
/// View model for the terminal-style chat view + skill rail + composer.
/// </summary>
public class ChatViewModel : ViewModelBase
{
    private readonly AppState _app;
    private readonly IDialogService _dialogs;

    public ChatViewModel(AppState app, IDialogService dialogs)
    {
        _app = app;
        _dialogs = dialogs;

        SendCommand = new AsyncRelayCommand(_ => SendAsync(), _ => CanSend);
        StopCommand = new RelayCommand(_ => Stop(), _ => _app.IsRunning);
        AttachFileCommand = new AsyncRelayCommand(_ => AttachAsync(), _ => !_app.IsRunning);
        PrimaryActionCommand = new AsyncRelayCommand(
            _ => _app.IsRunning ? Task.Run(Stop) : SendAsync(),
            _ => _app.IsRunning || CanSend);

        _app.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AppState.ActiveSession))
                LoadActiveSession();
            if (e.PropertyName is nameof(AppState.IsRunning) or nameof(AppState.ActiveSession))
                RaiseCommandState();
        };

        _app.ComposerAttachments.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasAttachments));
            RaiseCommandState();
        };

        Messages.CollectionChanged += (_, _) => RaiseStateProps();

        LoadActiveSession();
    }

    public AppState App => _app;

    public string GreetingText => "What should we build?";

    private string _sessionTitle = "New session";
    public string SessionTitle
    {
        get => _sessionTitle;
        set => SetField(ref _sessionTitle, value);
    }

    public string ComposerPlaceholder => HasMessages ? "Ask for follow-up changes" : "Do anything";

    public bool HasMessages => Messages.Count > 0;
    public bool IsEmptyState => Messages.Count == 0;

    // ----- Preview panel -----
    private PreviewViewModel? _preview;
    public PreviewViewModel? Preview
    {
        get => _preview;
        private set => SetField(ref _preview, value);
    }

    private bool _isPreviewOpen;
    public bool IsPreviewOpen
    {
        get => _isPreviewOpen;
        set
        {
            if (SetField(ref _isPreviewOpen, value))
            {
                OnPropertyChanged(nameof(ShowRightPanel));
                OnPropertyChanged(nameof(ShowSessionPanel));
                RightPanelChanged?.Invoke();
            }
        }
    }

    public bool ShowRightPanel => HasMessages || IsPreviewOpen;
    public bool ShowSessionPanel => HasMessages && !IsPreviewOpen;

    public event Action? RightPanelChanged;

    public RelayCommand ClosePreviewCommand => new(_ => IsPreviewOpen = false);

    public void OpenPreview(string path)
    {
        Preview = new PreviewViewModel(path);
        IsPreviewOpen = true;
    }

    public string ActiveSkillSummary
    {
        get
        {
            var n = _app.ActiveSession?.ActiveSkillIds.Count ?? 0;
            if (n == 0) return "No skills active";
            return n == 1 ? "1 skill active" : $"{n} skills active";
        }
    }

    private void RaiseStateProps()
    {
        OnPropertyChanged(nameof(HasMessages));
        OnPropertyChanged(nameof(IsEmptyState));
        OnPropertyChanged(nameof(ActiveSkillSummary));
        OnPropertyChanged(nameof(ComposerPlaceholder));
        OnPropertyChanged(nameof(ShowRightPanel));
        OnPropertyChanged(nameof(ShowSessionPanel));
        RightPanelChanged?.Invoke();
    }

    // ----- Skill rail -----
    public ObservableCollection<SkillChipViewModel> SkillChips { get; } = new();

    public bool HasSkills => SkillChips.Count > 0;
    public bool NoSkills => SkillChips.Count == 0;

    public void RebuildSkillRail()
    {
        SkillChips.Clear();
        var active = _app.ActiveSession?.ActiveSkillIds ?? new List<string>();
        foreach (var skill in _app.Skills)
        {
            SkillChips.Add(new SkillChipViewModel(skill, active.Contains(skill.Id), OnChipToggled));
        }
        OnPropertyChanged(nameof(HasSkills));
        OnPropertyChanged(nameof(NoSkills));
        RaiseCommandState();
    }

    private void OnChipToggled(SkillChipViewModel chip)
    {
        var session = _app.ActiveSession;
        if (session == null) return;

        if (chip.IsActive)
        {
            if (!session.ActiveSkillIds.Contains(chip.Id))
                session.ActiveSkillIds.Add(chip.Id);
        }
        else
        {
            session.ActiveSkillIds.Remove(chip.Id);
        }
        RaiseCommandState();
        RaiseStateProps();
    }

    // ----- Messages -----
    public ObservableCollection<MessageViewModel> Messages { get; } = new();

    public event Action? ScrollToEndRequested;

    private void LoadActiveSession()
    {
        Messages.Clear();
        var session = _app.ActiveSession;
        if (session != null)
        {
            SessionTitle = session.Name;
            foreach (var m in session.Messages)
            {
                var vm = new MessageViewModel(m, ResolveSkillName, OpenPreview) { Content = m.Content };
                Messages.Add(vm);
            }
        }
        RebuildSkillRail();
        ScrollToEndRequested?.Invoke();
    }

    private string ResolveSkillName(string id)
        => _app.Skills.FirstOrDefault(s => s.Id == id)?.Name ?? id;

    // ----- Composer -----
    private string _composerText = "";
    public string ComposerText
    {
        get => _composerText;
        set
        {
            if (SetField(ref _composerText, value))
                RaiseCommandState();
        }
    }

    public ObservableCollection<AttachmentViewModel> Attachments => _app.ComposerAttachments;
    public bool HasAttachments => _app.ComposerAttachments.Count > 0;

    public bool CanSend =>
        _app.ActiveSession is not null &&
        !string.IsNullOrWhiteSpace(ComposerText) &&
        !_app.IsRunning;

    public AsyncRelayCommand SendCommand { get; }
    public RelayCommand StopCommand { get; }
    public AsyncRelayCommand AttachFileCommand { get; }
    public AsyncRelayCommand PrimaryActionCommand { get; }

    public RelayCommand RemoveAttachmentCommand => new(p =>
    {
        if (p is AttachmentViewModel a) _app.ComposerAttachments.Remove(a);
    });

    public RelayCommand OpenOutputFolderCommand => new(_ =>
    {
        try
        {
            var folder = _app.OutputFolderPath;
            if (!string.IsNullOrWhiteSpace(folder) && System.IO.Directory.Exists(folder))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });
        }
        catch { /* ignore */ }
    });

    public System.Collections.ObjectModel.ObservableCollection<SkillChipViewModel> ActiveSkillChips => SkillChips;

    private void RaiseCommandState()
    {
        OnPropertyChanged(nameof(CanSend));
        OnPropertyChanged(nameof(HasAttachments));
        SendCommand.RaiseCanExecuteChanged();
        StopCommand.RaiseCanExecuteChanged();
        AttachFileCommand.RaiseCanExecuteChanged();
        PrimaryActionCommand.RaiseCanExecuteChanged();
    }

    private async Task AttachAsync()
    {
        var files = await _dialogs.PickFilesAsync("Attach files", allowMultiple: true);
        foreach (var f in files)
            _app.AddAttachment(f);
    }

    private void Stop()
    {
        try { _app.RunCts?.Cancel(); } catch { /* ignore */ }
    }

    private async Task SendAsync()
    {
        var session = _app.ActiveSession;
        if (session == null || !CanSend) return;

        if (_app.KiroStatus != KiroStatus.Ready)
        {
            AppendErrorMessage("kiro binary not found — configure path in Settings.");
            return;
        }

        var text = ComposerText.Trim();
        var attachmentPaths = _app.ComposerAttachments.Select(a => a.FullPath).ToList();
        var snapshot = session.ActiveSkillIds.ToList();

        // User message
        var userMsg = new Message
        {
            Role = "user",
            Content = text,
            AttachedFilePaths = attachmentPaths,
            ActiveSkillSnapshot = snapshot
        };
        session.Messages.Add(userMsg);
        Messages.Add(new MessageViewModel(userMsg, ResolveSkillName, OpenPreview) { Content = text });

        // Auto-name session on first message
        if (session.Messages.Count(m => m.Role == "user") == 1)
        {
            session.Name = Session.DeriveName(text);
            SessionTitle = session.Name;
            _app.Raise(nameof(AppState.Sessions));
        }

        // Reset composer
        ComposerText = "";
        _app.ComposerAttachments.Clear();

        // Assistant message (streamed)
        var assistantMsg = new Message { Role = "assistant", Content = "", ActiveSkillSnapshot = snapshot };
        session.Messages.Add(assistantMsg);
        var assistantVm = new MessageViewModel(assistantMsg, ResolveSkillName, OpenPreview) { Content = "" };
        Messages.Add(assistantVm);
        ScrollToEndRequested?.Invoke();

        _app.IsRunning = true;
        _app.RunCts = new CancellationTokenSource();
        RaiseCommandState();

        var outputFolder = _app.OutputFolderPath;
        var tempOut = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"lunachat-out-{Guid.NewGuid():N}");

        try
        {
            var promptFile = _app.PromptBuilder.BuildToTempFile(session, text, attachmentPaths);
            var runner = new KiroRunner(_app.KiroBinaryPath);

            await foreach (var line in runner.RunAsync(promptFile, tempOut, _app.RunCts.Token))
            {
                assistantMsg.Content += (assistantMsg.Content.Length > 0 ? "\n" : "") + line;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    assistantVm.Content = assistantMsg.Content;
                    ScrollToEndRequested?.Invoke();
                });
            }

            // Collect outputs
            if (!string.IsNullOrWhiteSpace(outputFolder))
            {
                var moved = KiroRunner.CollectOutputs(tempOut, outputFolder);
                assistantMsg.OutputFilePaths = moved;
                await Dispatcher.UIThread.InvokeAsync(() => assistantVm.Sync());
            }

            try { System.IO.File.Delete(promptFile); } catch { }
        }
        catch (OperationCanceledException)
        {
            assistantMsg.Content += "\n\n[stopped]";
            assistantVm.Content = assistantMsg.Content;
        }
        catch (KiroProcessException ex)
        {
            assistantVm.IsError = true;
            assistantMsg.Content = $"[ERROR] kiro exited with code {ex.ExitCode}\n\n{ex.StdErr}";
            assistantVm.Content = assistantMsg.Content;
        }
        catch (Exception ex)
        {
            assistantVm.IsError = true;
            assistantMsg.Content = $"[ERROR] {ex.Message}";
            assistantVm.Content = assistantMsg.Content;
            _dialogs.Toast($"Run failed: {ex.Message}");
        }
        finally
        {
            try { if (System.IO.Directory.Exists(tempOut)) System.IO.Directory.Delete(tempOut, true); } catch { }
            _app.IsRunning = false;
            _app.RunCts?.Dispose();
            _app.RunCts = null;
            RaiseCommandState();

            await _app.SessionStore.SaveAsync(session);
            ScrollToEndRequested?.Invoke();
        }
    }

    private void AppendErrorMessage(string text)
    {
        var msg = new Message { Role = "assistant", Content = text };
        var vm = new MessageViewModel(msg, ResolveSkillName, OpenPreview) { Content = text, IsError = true };
        Messages.Add(vm);
        ScrollToEndRequested?.Invoke();
    }
}
