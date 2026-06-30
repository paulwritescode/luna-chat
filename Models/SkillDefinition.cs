using System.IO;
using System.Text.RegularExpressions;

namespace LunaChat.Models;

/// <summary>
/// A skill loaded from the skills folder. Re-loaded from disk each launch (not persisted).
/// </summary>
public class SkillDefinition
{
    /// <summary>Slug derived from the file/folder name.</summary>
    public string Id { get; init; } = "";

    /// <summary>From YAML front-matter.</summary>
    public string Name { get; init; } = "";

    /// <summary>From YAML front-matter.</summary>
    public string Description { get; init; } = "";

    /// <summary>Full SKILL.md text (used in prompts).</summary>
    public string FullContent { get; init; } = "";

    /// <summary>Absolute path to the source file/folder.</summary>
    public string SourcePath { get; init; } = "";

    public DateTime LoadedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// e.g. "tech-resume-optimizer.skill" → "tech-resume-optimizer"
    ///      directory "ats-optimizer/" → "ats-optimizer"
    /// </summary>
    public static string SlugFrom(string path)
        => Path.GetFileNameWithoutExtension(Path.TrimEndingDirectorySeparator(path))
               .ToLowerInvariant()
               .Replace(" ", "-");

    /// <summary>
    /// Parse the leading YAML front-matter for name + description fields.
    /// Falls back gracefully when fields are missing.
    /// </summary>
    public static (string name, string description) ParseFrontMatter(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return ("Unknown Skill", "");

        var fmMatch = Regex.Match(
            content,
            @"^\s*---\s*\r?\n(.*?)\r?\n---",
            RegexOptions.Singleline);

        if (!fmMatch.Success)
            return ("Unknown Skill", "");

        var block = fmMatch.Groups[1].Value;

        var name = Regex.Match(block, @"(?m)^\s*name:\s*(.+?)\s*$").Groups[1].Value.Trim().Trim('"');
        var description = Regex.Match(block, @"(?m)^\s*description:\s*(.+?)\s*$").Groups[1].Value.Trim().Trim('"');

        return (
            string.IsNullOrEmpty(name) ? "Unknown Skill" : name,
            description);
    }
}
