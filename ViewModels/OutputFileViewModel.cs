using System.Diagnostics;
using System.IO;
using Avalonia.Input.Platform;
using Avalonia;

namespace LunaChat.ViewModels;

/// <summary>
/// Represents a kiro-produced output file shown in a banner / file list.
/// </summary>
public class OutputFileViewModel : ViewModelBase
{
    public OutputFileViewModel(string path)
    {
        FullPath = path;
    }

    public string FullPath { get; }

    public string FileName => Path.GetFileName(FullPath);

    public string Subtitle
    {
        get
        {
            var ext = Path.GetExtension(FullPath).TrimStart('.').ToUpperInvariant();
            return string.IsNullOrEmpty(ext) ? "File" : $"Document · {ext}";
        }
    }

    public string Directory => Path.GetDirectoryName(FullPath) ?? "";

    public string SavedToText => $"Saved to: {Directory}";

    public string SizeText
    {
        get
        {
            try
            {
                var len = new FileInfo(FullPath).Length;
                return HumanSize(len);
            }
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

    public RelayCommand OpenCommand => new(() => OpenFile());
    public RelayCommand RevealCommand => new(() => Reveal());
    public RelayCommand CopyPathCommand => new(() => CopyPath());

    public void OpenFile()
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = FullPath, UseShellExecute = true });
        }
        catch (Exception ex) { Console.Error.WriteLine(ex.Message); }
    }

    public void Reveal()
    {
        try
        {
            if (OperatingSystem.IsMacOS())
                Process.Start("open", $"-R \"{FullPath}\"");
            else if (OperatingSystem.IsWindows())
                Process.Start("explorer.exe", $"/select,\"{FullPath}\"");
            else
                Process.Start(new ProcessStartInfo { FileName = Directory, UseShellExecute = true });
        }
        catch (Exception ex) { Console.Error.WriteLine(ex.Message); }
    }

    public async void CopyPath()
    {
        try
        {
            var clipboard = (Application.Current?.ApplicationLifetime
                as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)
                ?.MainWindow?.Clipboard;
            if (clipboard != null)
                await clipboard.SetTextAsync(FullPath);
        }
        catch (Exception ex) { Console.Error.WriteLine(ex.Message); }
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
