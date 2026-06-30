using System.IO;

namespace LunaChat.Services;

/// <summary>
/// Locates the installed kiro-cli binary.
/// </summary>
public static class KiroLocator
{
    /// <summary>
    /// Attempts to locate the kiro binary, checking the configured path,
    /// then PATH entries, then common install locations.
    /// </summary>
    public static string? FindKiroBinary(string? configuredPath = null)
    {
        // 1. Check user-configured path from Settings
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
            return configuredPath;

        var exeName = OperatingSystem.IsWindows() ? "kiro.exe" : "kiro";
        var cliName = OperatingSystem.IsWindows() ? "kiro-cli.exe" : "kiro-cli";

        // 2. Search PATH entries (prefer kiro-cli, then kiro)
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        var separator = OperatingSystem.IsWindows() ? ';' : ':';
        var dirs = pathEnv.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var name in new[] { cliName, exeName })
        {
            foreach (var dir in dirs)
            {
                try
                {
                    var candidate = Path.Combine(dir.Trim(), name);
                    if (File.Exists(candidate))
                        return candidate;
                }
                catch
                {
                    // Ignore malformed PATH segments.
                }
            }
        }

        // 3. Common install locations
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var common = OperatingSystem.IsWindows()
            ? new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "kiro", "kiro-cli.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "kiro", "kiro.exe"),
            }
            : new[]
            {
                "/usr/local/bin/kiro-cli",
                "/opt/homebrew/bin/kiro-cli",
                "/usr/local/bin/kiro",
                "/opt/homebrew/bin/kiro",
                Path.Combine(home, ".local/bin/kiro-cli"),
                Path.Combine(home, ".local/bin/kiro"),
            };

        return common.FirstOrDefault(File.Exists);
    }
}
