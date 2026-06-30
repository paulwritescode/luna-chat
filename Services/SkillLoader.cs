using System.IO;
using System.IO.Compression;
using LunaChat.Models;

namespace LunaChat.Services;

/// <summary>
/// Loads skill definitions from the configured skills folder.
/// Supports `.skill` zip archives, directories containing SKILL.md,
/// and bare `*.skill.md` files.
/// </summary>
public class SkillLoader
{
    public async Task<List<SkillDefinition>> LoadAllAsync(string skillsDir)
    {
        var results = new List<SkillDefinition>();

        if (string.IsNullOrWhiteSpace(skillsDir) || !Directory.Exists(skillsDir))
            return results;

        foreach (var entry in Directory.EnumerateFileSystemEntries(skillsDir))
        {
            try
            {
                SkillDefinition? def = null;

                if (File.Exists(entry) && entry.EndsWith(".skill", StringComparison.OrdinalIgnoreCase))
                    def = await LoadFromZipAsync(entry);
                else if (File.Exists(entry) && entry.EndsWith(".skill.md", StringComparison.OrdinalIgnoreCase))
                    def = await LoadFromMarkdownAsync(entry);
                else if (Directory.Exists(entry))
                    def = await LoadFromDirectoryAsync(entry);

                if (def != null)
                    results.Add(def);
            }
            catch (Exception ex)
            {
                // Malformed skill — log and skip.
                Console.Error.WriteLine($"[SkillLoader] Skipping '{entry}': {ex.Message}");
            }
        }

        return results.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static async Task<SkillDefinition?> LoadFromDirectoryAsync(string dir)
    {
        var skillMd = Path.Combine(dir, "SKILL.md");
        if (!File.Exists(skillMd))
            return null;

        var content = await File.ReadAllTextAsync(skillMd);
        var (name, description) = SkillDefinition.ParseFrontMatter(content);

        return new SkillDefinition
        {
            Id = SkillDefinition.SlugFrom(dir),
            Name = name,
            Description = description,
            FullContent = content,
            SourcePath = dir
        };
    }

    private static async Task<SkillDefinition?> LoadFromMarkdownAsync(string file)
    {
        var content = await File.ReadAllTextAsync(file);
        var (name, description) = SkillDefinition.ParseFrontMatter(content);

        return new SkillDefinition
        {
            Id = SkillDefinition.SlugFrom(file.Replace(".skill.md", "")),
            Name = name,
            Description = description,
            FullContent = content,
            SourcePath = file
        };
    }

    private static async Task<SkillDefinition?> LoadFromZipAsync(string zipPath)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "lunachat-skill-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tempDir);
            ZipFile.ExtractToDirectory(zipPath, tempDir);

            // SKILL.md at root, or in the first subfolder.
            var skillMd = Path.Combine(tempDir, "SKILL.md");
            if (!File.Exists(skillMd))
            {
                skillMd = Directory
                    .EnumerateFiles(tempDir, "SKILL.md", SearchOption.AllDirectories)
                    .FirstOrDefault();
            }

            if (skillMd == null || !File.Exists(skillMd))
                return null;

            var content = await File.ReadAllTextAsync(skillMd);
            var (name, description) = SkillDefinition.ParseFrontMatter(content);

            return new SkillDefinition
            {
                Id = SkillDefinition.SlugFrom(zipPath),
                Name = name,
                Description = description,
                FullContent = content,
                SourcePath = zipPath
            };
        }
        finally
        {
            try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true); }
            catch { /* best effort cleanup */ }
        }
    }
}
