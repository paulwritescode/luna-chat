using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Threading;
using LunaChat.Services;

namespace LunaChat.ViewModels;

public class FileEntryViewModel : ViewModelBase
{
    public FileEntryViewModel(string path)
    {
        FullPath = path;
    }

    public string FullPath { get; }
    public string FileName => Path.GetFileName(FullPath);

    public string SizeText
    {
        get
        {
            try { return HumanSize(new FileInfo(FullPath).Length); }
            catch { return "—"; }
        }
    }

    public string ModifiedText
    {
        get
        {
            try { return File.GetLastWriteTime(FullPath).ToString("dd MMM HH:mm"); }
            catch { return "—"; }
        }
    }

    private static string HumanSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
        double size = bytes;
        int u = 0;
        while (size >= 1024 && u < units.Length - 1) { size /= 1024; u++; }
        return $"{size:0.#} {units[u]}";
    }
}

/// <summary>
/// Two-column input/output folder browser.
/// </summary>
public class FileBrowserViewModel : ViewModelBase
{
    private readonly AppState _app;
    private readonly IDialogService _dialogs;
    private readonly OutputFolderWatcher _watcher = new();

    public FileBrowserViewModel(AppState app, IDialogService dialogs)
    {
        _app = app;
        _dialogs = dialogs;

        _watcher.FileCreated += _ => Dispatcher.UIThread.Post(RefreshOutputs);

        ChangeInputCommand = new AsyncRelayCommand(_ => ChangeInputAsync());
        ChangeOutputCommand = new AsyncRelayCommand(_ => ChangeOutputAsync());
        ClearOutputsCommand = new AsyncRelayCommand(_ => ClearOutputsAsync());
    }

    public ObservableCollection<FileEntryViewModel> InputFiles { get; } = new();
    public ObservableCollection<OutputFileViewModel> OutputFiles { get; } = new();

    public bool HasInputFiles => InputFiles.Count > 0;
    public bool HasOutputFiles => OutputFiles.Count > 0;
    public bool NoInputFiles => InputFiles.Count == 0;
    public bool NoOutputFiles => OutputFiles.Count == 0;

    public string InputFolder => _app.InputFolderPath;
    public string OutputFolder => _app.OutputFolderPath;

    public AsyncRelayCommand ChangeInputCommand { get; }
    public AsyncRelayCommand ChangeOutputCommand { get; }
    public AsyncRelayCommand ClearOutputsCommand { get; }

    public RelayCommand UseInChatCommand => new(p =>
    {
        if (p is FileEntryViewModel f)
        {
            if (_app.AddAttachment(f.FullPath))
                _dialogs.Toast($"Attached {f.FileName} to chat");
            else
                _dialogs.Toast("Attachment limit reached (max 3)");
        }
    });

    public AsyncRelayCommand DeleteOutputCommand => new(async p =>
    {
        if (p is OutputFileViewModel f)
        {
            var ok = await _dialogs.ConfirmAsync("Delete file", $"Delete {f.FileName}?");
            if (!ok) return;
            try { File.Delete(f.FullPath); } catch (Exception ex) { _dialogs.Toast(ex.Message); }
            RefreshOutputs();
        }
    });

    public void Refresh()
    {
        OnPropertyChanged(nameof(InputFolder));
        OnPropertyChanged(nameof(OutputFolder));
        RefreshInputs();
        RefreshOutputs();
        if (Directory.Exists(OutputFolder))
            _watcher.Watch(OutputFolder);
    }

    private void RefreshInputs()
    {
        InputFiles.Clear();
        if (Directory.Exists(InputFolder))
        {
            foreach (var f in Directory.EnumerateFiles(InputFolder).OrderBy(Path.GetFileName))
                InputFiles.Add(new FileEntryViewModel(f));
        }
        OnPropertyChanged(nameof(HasInputFiles));
        OnPropertyChanged(nameof(NoInputFiles));
    }

    private void RefreshOutputs()
    {
        OutputFiles.Clear();
        if (Directory.Exists(OutputFolder))
        {
            foreach (var f in Directory.EnumerateFiles(OutputFolder).OrderBy(Path.GetFileName))
                OutputFiles.Add(new OutputFileViewModel(f));
        }
        OnPropertyChanged(nameof(HasOutputFiles));
        OnPropertyChanged(nameof(NoOutputFiles));
    }

    private async Task ChangeInputAsync()
    {
        var folder = await _dialogs.PickFolderAsync("Select input folder");
        if (folder == null) return;
        if (_app.ActiveSession != null) _app.ActiveSession.InputFolder = folder;
        else _app.Settings.DefaultInputFolder = folder;
        Refresh();
    }

    private async Task ChangeOutputAsync()
    {
        var folder = await _dialogs.PickFolderAsync("Select output folder");
        if (folder == null) return;
        if (_app.ActiveSession != null) _app.ActiveSession.OutputFolder = folder;
        else _app.Settings.DefaultOutputFolder = folder;
        Refresh();
    }

    private async Task ClearOutputsAsync()
    {
        var ok = await _dialogs.ConfirmAsync("Clear all outputs",
            $"Delete all files in {OutputFolder}? This cannot be undone.");
        if (!ok) return;

        if (Directory.Exists(OutputFolder))
        {
            foreach (var f in Directory.EnumerateFiles(OutputFolder))
            {
                try { File.Delete(f); } catch (Exception ex) { _dialogs.Toast(ex.Message); }
            }
        }
        RefreshOutputs();
    }
}
