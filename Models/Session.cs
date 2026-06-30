namespace LunaChat.Models;

/// <summary>
/// A chat session. Persisted as JSON in the platform data directory.
/// </summary>
public class Session
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Auto-generated from the first user message.</summary>
    public string Name { get; set; } = "New Session";

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary><see cref="SkillDefinition.Id"/> values active for this session.</summary>
    public List<string> ActiveSkillIds { get; set; } = new();

    /// <summary>Folder override (null = use AppSettings default).</summary>
    public string? InputFolder { get; set; }

    /// <summary>Folder override (null = use AppSettings default).</summary>
    public string? OutputFolder { get; set; }

    public List<Message> Messages { get; set; } = new();

    /// <summary>
    /// Derive a friendly session name from the first user message text.
    /// </summary>
    public static string DeriveName(string firstMessage)
    {
        var cleaned = firstMessage.Replace("\r", " ").Replace("\n", " ").Trim();
        if (cleaned.Length <= 40)
            return cleaned.Length == 0 ? "New Session" : cleaned;
        return cleaned[..40].TrimEnd() + "…";
    }
}
