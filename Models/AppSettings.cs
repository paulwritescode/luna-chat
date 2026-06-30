namespace LunaChat.Models;

/// <summary>
/// User configuration, persisted as JSON at {DataDir}/settings.json.
/// </summary>
public class AppSettings
{
    // kiro binary
    public string KiroBinaryPath { get; set; } = "";
    public string KiroConfigPath { get; set; } = "";

    // Folders
    public string SkillsFolder { get; set; } = "";
    public string DefaultInputFolder { get; set; } = "";
    public string DefaultOutputFolder { get; set; } = "";

    // Appearance
    public int FontSize { get; set; } = 13;
    public string MonoFont { get; set; } = "JetBrains Mono";

    /// <summary>"System" | "Light" | "Dark"</summary>
    public string Theme { get; set; } = "System";
}
