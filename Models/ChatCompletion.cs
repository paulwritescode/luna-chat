namespace LunaChat.Models;

/// <summary>One turn in a chat request sent to a model provider.</summary>
public class ChatTurn
{
    public string Role { get; set; } = "user"; // "user" | "assistant" | "system"
    public string Content { get; set; } = "";

    public ChatTurn() { }
    public ChatTurn(string role, string content) { Role = role; Content = content; }
}

/// <summary>Result of a provider credential check.</summary>
public readonly struct VerifyResult
{
    public bool Ok { get; init; }
    public string? Error { get; init; }

    public static VerifyResult Success => new() { Ok = true };
    public static VerifyResult Fail(string error) => new() { Ok = false, Error = error };
}
