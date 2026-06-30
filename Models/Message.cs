namespace LunaChat.Models;

/// <summary>
/// A single chat message within a <see cref="Session"/>.
/// </summary>
public class Message
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>"user" | "assistant"</summary>
    public string Role { get; init; } = "";

    public string Content { get; set; } = "";

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>Snapshot of the active skill ids at send time (for header display).</summary>
    public List<string> ActiveSkillSnapshot { get; init; } = new();

    /// <summary>Input files attached to the message.</summary>
    public List<string> AttachedFilePaths { get; set; } = new();

    /// <summary>Files produced by kiro for this message.</summary>
    public List<string> OutputFilePaths { get; set; } = new();
}
