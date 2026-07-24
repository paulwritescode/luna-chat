namespace LunaChat.Models;

public enum McpTransport
{
    Stdio,
    Sse
}

/// <summary>
/// A Model Context Protocol tool server the user has registered. Stored locally
/// in mcp.json. Execution wiring (exposing these tools to the model loop) is a
/// later step — this is the management/config surface.
/// </summary>
public class McpServer
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public McpTransport Transport { get; set; } = McpTransport.Stdio;

    // stdio transport
    public string Command { get; set; } = "";
    public string Args { get; set; } = "";

    // sse transport
    public string Url { get; set; } = "";

    public bool Enabled { get; set; } = true;
}

public class McpFile
{
    public List<McpServer> Servers { get; set; } = new();
}
