using System.IO;
using Avalonia.Media.Imaging;

namespace LunaChat.Services;

/// <summary>
/// Renders PDF pages to Avalonia bitmaps via PDFtoImage (PDFium).
/// All methods fail soft (return null / empty) so the UI can fall back gracefully.
/// </summary>
public static class PdfThumbnailer
{
    public static bool IsPdf(string path)
        => Path.GetExtension(path).Equals(".pdf", StringComparison.OrdinalIgnoreCase);

    public static int GetPageCount(string path)
    {
        try
        {
            using var s = File.OpenRead(path);
            return PDFtoImage.Conversion.GetPageCount(s);
        }
        catch { return 0; }
    }

    /// <summary>Render a single page to a bitmap, optionally constrained to a width.</summary>
    public static Bitmap? RenderPage(string path, int page, int? decodeWidth = null)
    {
        try
        {
            using var pdf = File.OpenRead(path);
            using var ms = new MemoryStream();
            PDFtoImage.Conversion.SavePng(ms, pdf, page);
            ms.Position = 0;
            return decodeWidth is int w ? Bitmap.DecodeToWidth(ms, w) : new Bitmap(ms);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Render up to <paramref name="maxPages"/> pages for the preview pane.</summary>
    public static List<Bitmap> RenderPages(string path, int maxPages = 12)
    {
        var result = new List<Bitmap>();
        try
        {
            var count = GetPageCount(path);
            if (count <= 0) return result;

            var pages = Math.Min(count, maxPages);
            for (int i = 0; i < pages; i++)
            {
                var bmp = RenderPage(path, i, decodeWidth: 1000);
                if (bmp != null) result.Add(bmp);
            }
        }
        catch { /* fall through */ }
        return result;
    }
}
