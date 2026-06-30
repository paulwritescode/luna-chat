using System.IO;

namespace LunaChat.Services;

/// <summary>
/// Watches an output folder and raises <see cref="FileCreated"/> when kiro writes files.
/// </summary>
public class OutputFolderWatcher : IDisposable
{
    private FileSystemWatcher? _watcher;

    public event Action<string>? FileCreated;

    public void Watch(string path)
    {
        Dispose();

        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return;

        _watcher = new FileSystemWatcher(path)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };
        _watcher.Created += (_, e) => FileCreated?.Invoke(e.FullPath);
    }

    public void Dispose()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }
        GC.SuppressFinalize(this);
    }
}
