using System.IO;
using System.Text;
using LunaChat.Models;

namespace LunaChat.Services;

/// <summary>
/// Assembles the full kiro prompt (active skills + history + current task + attachments)
/// and writes it to a temp file.
/// </summary>
public class PromptBuilder
{
    private readonly Func<string, SkillDefinition?> _skillResolver;

    /// <param name="skillResolver">Resolves a skill id to its definition.</param>
    public PromptBuilder(Func<string, SkillDefinition?> skillResolver)
    {
        _skillResolver = skillResolver;
    }

    /// <summary>
    /// Builds the prompt text for a session + current message.
    /// </summary>
    public string Build(Session session, string currentMessage, IEnumerable<string> attachedFilePaths)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# luna-chat Session");
        sb.AppendLine();

        // Skills block
        sb.AppendLine("## System: Active Skills");
        sb.AppendLine();
        foreach (var skillId in session.ActiveSkillIds)
        {
            var skill = _skillResolver(skillId);
            if (skill == null) continue;
            sb.AppendLine($"### {skill.Name}");
            sb.AppendLine();
            sb.AppendLine(skill.FullContent);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        // Conversation history (last 20 messages to avoid context overflow)
        sb.AppendLine("## Conversation History");
        sb.AppendLine();
        foreach (var msg in session.Messages.TakeLast(20))
        {
            var role = msg.Role == "user" ? "**User**" : "**Assistant**";
            sb.AppendLine($"{role}: {msg.Content}");
            sb.AppendLine();
        }

        // Current message
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Current Task");
        sb.AppendLine();
        sb.AppendLine(currentMessage);

        // Attached files
        var paths = attachedFilePaths?.ToList() ?? new List<string>();
        if (paths.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Attached Files");
            sb.AppendLine();
            foreach (var path in paths)
            {
                sb.AppendLine($"### {Path.GetFileName(path)}");
                sb.AppendLine("```");
                try
                {
                    sb.AppendLine(File.ReadAllText(path));
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"[could not read file: {ex.Message}]");
                }
                sb.AppendLine("```");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds the prompt and writes it to /tmp/lunachat-prompt-{uuid}.md.
    /// Returns the temp file path.
    /// </summary>
    public string BuildToTempFile(Session session, string currentMessage, IEnumerable<string> attachedFilePaths)
    {
        var content = Build(session, currentMessage, attachedFilePaths);
        var path = Path.Combine(Path.GetTempPath(), $"lunachat-prompt-{Guid.NewGuid():N}.md");
        File.WriteAllText(path, content);
        return path;
    }
}
