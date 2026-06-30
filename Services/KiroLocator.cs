using System.IO;

namespace LunaChat.Services;

/// <summary>
/// Locates the headless kiro-cli binary.
/// IMPORTANT: plain `kiro` on PATH is usually the IDE launcher (it opens the app),
/// so we always prefer `kiro-cli`, which is the headless command used for chat.
/// </summary>
public static class KiroLocator
{
    public static string? FindKiroBinary(string? configuredPath = null)
    {
        var cliName = OperatingSystem.IsWindows() ? "kiro-cli.exe" : "kiro-cli";

        // 1. Always prefer kiro-cli wherever it is found.
        var cli = FindOnPath(cliName) ?? FindInCommonLocations(cliName);
        if (cli != null) return cli;

        // 2. Honor an explicitly configured path that points at a real CLI.
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath)
            && Path.GetFileName(configuredPath).Contains("kiro-cli", StringComparison.OrdinalIgnoreCase))
            return configuredPath;

        // 3. Last resort: plain kiro (may be the IDE launcher on some installs).
        var exeName = OperatingSystem.IsWindows() ? "kiro.exe" : "kiro";
        return FindOnPath(exeName) ?? FindInCommonLocations(exeName)
            ?? (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath) ? configuredPath : null);
    }

    private static string? FindOnPath(string name)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        var separator = OperatingSystem.IsWindows() ? ';' : ':';
        foreach (var dir in pathEnv.Split(separator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(dir.Trim(), name);
                if (File.Exists(candidate)) return candidate;
            }
            catch { /* ignore malformed PATH segments */ }
        }
        return null;
    }

    private static string? FindInCommonLocations(string name)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string[] candidates = OperatingSystem.IsWindows()
            ? new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "kiro-cli", name),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "kiro", name),
            }
            : new[]
            {
                Path.Combine(home, ".local/bin", name),
                $"/Applications/Kiro CLI.app/Contents/MacOS/{name}",
                $"/usr/local/bin/{name}",
                $"/opt/homebrew/bin/{name}",
            };

        return candidates.FirstOrDefault(File.Exists);
    }
}
