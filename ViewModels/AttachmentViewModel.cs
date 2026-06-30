using System.IO;
using Avalonia.Media.Imaging;
using LunaChat.Services;

namespace LunaChat.ViewModels;

/// <summary>
/// A file attached to the composer. Shows a portrait thumbnail for images and PDFs,
/// or a typed placeholder card for other documents. Removable via <see cref="RemoveCommand"/>.
/// </summary>
public class AttachmentViewModel : ViewModelBase
{
    private static readonly string[] ImageExt = { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp" };

    private readonly Action<AttachmentViewModel>? _onRemove;

    public AttachmentViewModel(string path, Action<AttachmentViewModel>? onRemove = null)
    {
        FullPath = path;
        _onRemove = onRemove;

        var ext = Path.GetExtension(path).ToLowerInvariant();
        IsImage = ImageExt.Contains(ext);
        IsPdf = ext == ".pdf";
        Ext = string.IsNullOrEmpty(ext) ? "FILE" : ext.TrimStart('.').ToUpperInvariant();

        if (IsImage) TryLoadImageThumb(path);
        else if (IsPdf) Thumbnail = PdfThumbnailer.RenderPage(path, 0, decodeWidth: 220);
    }

    /// <summary>Whether the remove (X) affordance is shown (composer = yes, message = no).</summary>
    public bool ShowRemove => _onRemove != null;

    public string FullPath { get; }

    /// <summary>File name only (no path) for display.</summary>
    public string FileName => Path.GetFileName(FullPath);

    public string Ext { get; }

    public bool IsImage { get; }
    public bool IsPdf { get; }

    /// <summary>Show the placeholder card only when there is no thumbnail.</summary>
    public bool IsDocument => Thumbnail == null;

    private Bitmap? _thumbnail;
    public Bitmap? Thumbnail
    {
        get => _thumbnail;
        private set
        {
            if (SetField(ref _thumbnail, value))
            {
                OnPropertyChanged(nameof(HasThumbnail));
                OnPropertyChanged(nameof(IsDocument));
            }
        }
    }

    public bool HasThumbnail => _thumbnail != null;

    public RelayCommand RemoveCommand => new(_ => _onRemove?.Invoke(this));

    private void TryLoadImageThumb(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            Thumbnail = Bitmap.DecodeToWidth(stream, 220);
        }
        catch
        {
            // Fall back to the document card if decoding fails.
        }
    }
}
