using System.IO;
using Avalonia.Media.Imaging;

namespace LunaChat.ViewModels;

/// <summary>
/// A file attached to the composer. Shows a portrait thumbnail for images,
/// or a typed placeholder card for documents. Removable via <see cref="RemoveCommand"/>.
/// </summary>
public class AttachmentViewModel : ViewModelBase
{
    private static readonly string[] ImageExt = { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp" };

    private readonly Action<AttachmentViewModel> _onRemove;

    public AttachmentViewModel(string path, Action<AttachmentViewModel> onRemove)
    {
        FullPath = path;
        _onRemove = onRemove;

        var ext = Path.GetExtension(path).ToLowerInvariant();
        IsImage = ImageExt.Contains(ext);
        Ext = string.IsNullOrEmpty(ext) ? "FILE" : ext.TrimStart('.').ToUpperInvariant();

        if (IsImage)
            TryLoadThumbnail(path);
    }

    public string FullPath { get; }

    /// <summary>File name only (no path) for display.</summary>
    public string FileName => Path.GetFileName(FullPath);

    public string Ext { get; }

    public bool IsImage { get; }

    public bool IsDocument => !IsImage;

    private Bitmap? _thumbnail;
    public Bitmap? Thumbnail
    {
        get => _thumbnail;
        private set
        {
            if (SetField(ref _thumbnail, value))
                OnPropertyChanged(nameof(HasThumbnail));
        }
    }

    public bool HasThumbnail => _thumbnail != null;

    public RelayCommand RemoveCommand => new(_ => _onRemove(this));

    private void TryLoadThumbnail(string path)
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
