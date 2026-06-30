using System.IO;

namespace LunaChat.Services;

/// <summary>
/// Resolves platform-specific data directories for luna-chat.
/// Mac:     ~/Library/Application Support/LunaChat/
/// Windows: %APPDATA%\LunaChat\
/// </summary>
public static class PlatformDirs
{
    public const string AppFolderName = "LunaChat";

    public static string DataDir
    {
        get
        {
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(baseDir, AppFolderName);
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string SessionsDir
    {
        get
        {
            var dir = Path.Combine(DataDir, "sessions");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string SettingsPath => Path.Combine(DataDir, "settings.json");
}
