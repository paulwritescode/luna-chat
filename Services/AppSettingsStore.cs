using System.IO;
using System.Text.Json;
using LunaChat.Models;

namespace LunaChat.Services;

/// <summary>
/// JSON persistence for <see cref="AppSettings"/>.
/// </summary>
public class AppSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AppSettings Load()
    {
        try
        {
            var path = PlatformDirs.SettingsPath;
            if (!File.Exists(path))
                return CreateDefaults();

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? CreateDefaults();
        }
        catch
        {
            return CreateDefaults();
        }
    }

    public async Task SaveAsync(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await File.WriteAllTextAsync(PlatformDirs.SettingsPath, json);
    }

    private static AppSettings CreateDefaults()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var settings = new AppSettings
        {
            KiroBinaryPath = KiroLocator.FindKiroBinary() ?? "",
            DefaultOutputFolder = Path.Combine(home, "Documents", PlatformDirs.AppFolderName, "outputs")
        };
        return settings;
    }
}
