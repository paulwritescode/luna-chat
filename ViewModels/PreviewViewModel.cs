using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Avalonia.Media.Imaging;
using LunaChat.Services;

namespace LunaChat.ViewModels;

/// <summary>
/// Backs the resizable in-app file preview pane.
/// Renders PDFs (page images), markdown, plain text, or an image depending on the file type.
/// </summary>
public class PreviewViewModel : ViewModelBase
{
    private static readonly string[] ImageExt = { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp" };
    private static readonly string[] MarkdownExt = { ".md", ".markdown" };

    public PreviewViewModel(string path)
    {
        FullPath = path;
        var ext = Path.GetExtension(path).ToLowerInvariant();

        IsImage = ImageExt.Contains(ext);
        IsMarkdown = MarkdownExt.Contains(ext);
        IsPdf = ext == ".pdf";

        if (IsImage)
        {
            try { using var s = File.OpenRead(path); Image = Bitmap.DecodeToWidth(s, 1400); }
            catch { IsImage = false; }
        }

        if (IsPdf)
        {
            foreach (var page in PdfThumbnailer.RenderPages(path))
                PdfPages.Add(page);
            if (PdfPages.Count == 0) IsPdf = false;
        }

        if (!IsImage && !IsPdf)
        {
            try { TextContent = File.ReadAllText(path); }
            catch (Exception ex) { TextContent = $"[could not read file: {ex.Message}]"; IsMarkdown = false; }
        }

        IsText = !IsImage && !IsMarkdown && !IsPdf;
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

    public bool IsImage { get; private set; }
    public bool IsMarkdown { get; private set; }
    public bool IsText { get; private set; }
    public bool IsPdf { get; private set; }

    public Bitmap? Image { get; }
    public ObservableCollection<Bitmap> PdfPages { get; } = new();
    public string TextContent { get; private set; } = "";

    public RelayCommand OpenExternalCommand => new(_ =>
    {
        try { Process.Start(new ProcessStartInfo { FileName = FullPath, UseShellExecute = true }); }
        catch { /* ignore */ }
    });
}
