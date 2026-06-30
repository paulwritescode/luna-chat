using System.IO;
using System.Text.Json;
using LunaChat.Models;

namespace LunaChat.Services;

/// <summary>
/// JSON persistence for sessions, one file per session in the platform data dir.
/// </summary>
public class SessionStore
{
    private readonly string _dir;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SessionStore()
    {
        _dir = PlatformDirs.SessionsDir;
    }

    public async Task SaveAsync(Session session)
    {
        session.UpdatedAt = DateTime.UtcNow;
        var path = Path.Combine(_dir, $"{session.Id}.json");
        var json = JsonSerializer.Serialize(session, JsonOptions);
        await File.WriteAllTextAsync(path, json);
    }

    public async Task<List<Session>> LoadAllAsync()
    {
        var sessions = new List<Session>();
        foreach (var file in Directory.EnumerateFiles(_dir, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var session = JsonSerializer.Deserialize<Session>(json, JsonOptions);
                if (session != null) sessions.Add(session);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SessionStore] Skipping '{file}': {ex.Message}");
            }
        }
        return sessions.OrderByDescending(s => s.UpdatedAt).ToList();
    }

    public void Delete(Session session)
    {
        var path = Path.Combine(_dir, $"{session.Id}.json");
        if (File.Exists(path)) File.Delete(path);
    }
}
