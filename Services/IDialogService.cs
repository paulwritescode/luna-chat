namespace LunaChat.Services;

/// <summary>
/// Abstraction over OS file/folder pickers and confirmation dialogs,
/// implemented by the main window so view models stay UI-framework agnostic.
/// </summary>
public interface IDialogService
{
    Task<IReadOnlyList<string>> PickFilesAsync(string title, bool allowMultiple);
    Task<string?> PickFolderAsync(string title);
    Task<bool> ConfirmAsync(string title, string message);
    void Toast(string message);
}
