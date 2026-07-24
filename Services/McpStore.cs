using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using LunaChat.Models;

namespace LunaChat.Services;

/// <summary>Local CRUD persistence for registered MCP servers (mcp.json).</summary>
public class McpStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private McpFile _file;

    public McpStore() => _file = Load();

    private static string FilePath => Path.Combine(PlatformDirs.DataDir, "mcp.json");

    private static McpFile Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new McpFile();
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<McpFile>(json, JsonOptions) ?? new McpFile();
        }
        catch { return new McpFile(); }
    }

    private async Task PersistAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_file, JsonOptions);
            await File.WriteAllTextAsync(FilePath, json);
        }
        catch { /* best effort */ }
    }

    public IReadOnlyList<McpServer> Servers => _file.Servers;

    public async Task UpsertAsync(McpServer server)
    {
        var existing = _file.Servers.FirstOrDefault(s => s.Id == server.Id);
        if (existing == null) _file.Servers.Add(server);
        else
        {
            existing.Name = server.Name;
            existing.Transport = server.Transport;
            existing.Command = server.Command;
            existing.Args = server.Args;
            existing.Url = server.Url;
            existing.Enabled = server.Enabled;
        }
        await PersistAsync();
    }

    public async Task RemoveAsync(string id)
    {
        _file.Servers.RemoveAll(s => s.Id == id);
        await PersistAsync();
    }

    public async Task SetEnabledAsync(string id, bool enabled)
    {
        var s = _file.Servers.FirstOrDefault(x => x.Id == id);
        if (s == null) return;
        s.Enabled = enabled;
        await PersistAsync();
    }
}
