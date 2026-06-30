using System.Collections.ObjectModel;
using Avalonia.Input.Platform;
using LunaChat.Models;

namespace LunaChat.ViewModels;

/// <summary>
/// Display wrapper around a <see cref="Message"/> for the chat log.
/// </summary>
public class MessageViewModel : ViewModelBase
{
    public MessageViewModel(Message message, Func<string, string> skillNameResolver,
        Action<string>? openPreview = null,
        Action<MessageViewModel>? onEdit = null,
        Action<MessageViewModel>? onRedo = null,
        Action<MessageViewModel>? openResponsePreview = null)
    {
        Model = message;
        IsUser = message.Role == "user";
        _openPreview = openPreview;
        _onEdit = onEdit;
        _onRedo = onRedo;
        _openResponsePreview = openResponsePreview;

        foreach (var id in message.ActiveSkillSnapshot)
            SkillNames.Add(skillNameResolver(id));

        foreach (var f in message.OutputFilePaths)
            OutputFiles.Add(new OutputFileViewModel(f));

        foreach (var f in message.AttachedFilePaths)
            Attachments.Add(new AttachmentViewModel(f));
    }

    private readonly Action<string>? _openPreview;
    private readonly Action<MessageViewModel>? _onEdit;
    private readonly Action<MessageViewModel>? _onRedo;
    private readonly Action<MessageViewModel>? _openResponsePreview;

    public Message Model { get; }

    public bool IsUser { get; }

    public string RoleLabel => IsUser ? "you" : "kiro";

    public string TimeText => Model.Timestamp.ToLocalTime().ToString("HH:mm");

    public ObservableCollection<string> SkillNames { get; } = new();

    public bool HasSkillChips => SkillNames.Count > 0;

    public ObservableCollection<AttachmentViewModel> Attachments { get; } = new();
    public bool HasAttachments => Attachments.Count > 0;

    public RelayCommand PreviewCommand => new(p =>
    {
        if (p is string path) _openPreview?.Invoke(path);
        else if (p is OutputFileViewModel o) _openPreview?.Invoke(o.FullPath);
    });

    public RelayCommand EditCommand => new(_ => _onEdit?.Invoke(this));
    public RelayCommand RedoCommand => new(_ => _onRedo?.Invoke(this));
    public RelayCommand OpenResponsePreviewCommand => new(_ => _openResponsePreview?.Invoke(this));

    /// <summary>Heuristic: does the assistant text look like rich markdown worth a preview card?</summary>
    public bool LooksLikeMarkdown
    {
        get
        {
            if (IsUser || string.IsNullOrWhiteSpace(Content)) return false;
            var c = Content;
            return c.Contains("\n#") || c.StartsWith("#")
                || c.Contains("\n- ") || c.Contains("\n* ")
                || c.Contains("```") || c.Contains("\n|")
                || c.Length > 600;
        }
    }

    private string _content = "";
    public string Content
    {
        get => _content;
        set
        {
            if (SetField(ref _content, value))
                OnPropertyChanged(nameof(LooksLikeMarkdown));
        }
    }

    public ObservableCollection<OutputFileViewModel> OutputFiles { get; } = new();

    public bool HasOutputs => OutputFiles.Count > 0;

    public RelayCommand CopyCommand => new(async _ =>
    {
        try
        {
            var clipboard = (Avalonia.Application.Current?.ApplicationLifetime
                as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)
                ?.MainWindow?.Clipboard;
            if (clipboard != null) await clipboard.SetTextAsync(Content);
        }
        catch { /* ignore */ }
    });

    private bool _isError;
    public bool IsError
    {
        get => _isError;
        set => SetField(ref _isError, value);
    }

    public void Sync()
    {
        Content = Model.Content;
        OutputFiles.Clear();
        foreach (var f in Model.OutputFilePaths)
            OutputFiles.Add(new OutputFileViewModel(f));
        OnPropertyChanged(nameof(HasOutputs));
    }
}
