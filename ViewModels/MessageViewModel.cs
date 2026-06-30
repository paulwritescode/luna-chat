using System.Collections.ObjectModel;
using Avalonia.Input.Platform;
using LunaChat.Models;

namespace LunaChat.ViewModels;

/// <summary>
/// Display wrapper around a <see cref="Message"/> for the chat log.
/// </summary>
public class MessageViewModel : ViewModelBase
{
    public MessageViewModel(Message message, Func<string, string> skillNameResolver, Action<string>? openPreview = null)
    {
        Model = message;
        IsUser = message.Role == "user";
        _openPreview = openPreview;

        foreach (var id in message.ActiveSkillSnapshot)
            SkillNames.Add(skillNameResolver(id));

        foreach (var f in message.OutputFilePaths)
            OutputFiles.Add(new OutputFileViewModel(f));

        foreach (var f in message.AttachedFilePaths)
            AttachedNames.Add(System.IO.Path.GetFileName(f));
    }

    private readonly Action<string>? _openPreview;

    public Message Model { get; }

    public bool IsUser { get; }

    public string RoleLabel => IsUser ? "you" : "kiro";

    public string TimeText => Model.Timestamp.ToLocalTime().ToString("HH:mm");

    public ObservableCollection<string> SkillNames { get; } = new();

    public bool HasSkillChips => SkillNames.Count > 0;

    public ObservableCollection<string> AttachedNames { get; } = new();
    public bool HasAttachments => AttachedNames.Count > 0;

    public RelayCommand PreviewCommand => new(p =>
    {
        if (p is string path) _openPreview?.Invoke(path);
        else if (p is OutputFileViewModel o) _openPreview?.Invoke(o.FullPath);
    });

    private string _content = "";
    public string Content
    {
        get => _content;
        set => SetField(ref _content, value);
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
