using System.IO;
using System.Text.Json;
using LunaChat.Models;

namespace LunaChat.Services;

/// <summary>
/// Owns connector configuration: non-secret state in connectors.json, tokens in
/// the OS credential vault (under a "connector.{id}" account so they never collide
/// with model-provider keys).
/// </summary>
public class ConnectorStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ISecretStore _secrets;
    private ConnectorsFile _file;

    public ConnectorStore(ISecretStore? secrets = null)
    {
        _secrets = secrets ?? SecretStoreFactory.Instance;
        _file = Load();
    }

    private static string FilePath => Path.Combine(PlatformDirs.DataDir, "connectors.json");
    private static string Account(string id) => $"connector.{id}";

    private static ConnectorsFile Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new ConnectorsFile();
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<ConnectorsFile>(json, JsonOptions) ?? new ConnectorsFile();
        }
        catch { return new ConnectorsFile(); }
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

    private ConnectorState? StateFor(string id) => _file.Connectors.FirstOrDefault(c => c.Id == id);

    public bool IsConnected(string id) => StateFor(id)?.Connected ?? false;

    public string ValueOr(string id, string key, string fallback)
    {
        var st = StateFor(id);
        if (st != null && st.Values.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v))
            return v;
        return fallback;
    }

    /// <summary>Store fields: secret → vault, rest → json; mark connected.</summary>
    public async Task ConnectAsync(ConnectorDef def, IReadOnlyDictionary<string, string> fields)
    {
        var st = StateFor(def.Id);
        if (st == null)
        {
            st = new ConnectorState { Id = def.Id };
            _file.Connectors.Add(st);
        }

        foreach (var f in def.Fields)
        {
            if (!fields.TryGetValue(f.Key, out var value)) continue;
            value = value?.Trim() ?? "";
            if (f.Secret)
            {
                if (value.Length > 0) await _secrets.SetAsync(Account(def.Id), value);
            }
            else
            {
                st.Values[f.Key] = value;
            }
        }

        st.Connected = true;
        await PersistAsync();
    }

    public async Task DisconnectAsync(string id)
    {
        try { await _secrets.DeleteAsync(Account(id)); } catch { /* ignore */ }
        _file.Connectors.RemoveAll(c => c.Id == id);
        await PersistAsync();
    }

    public int ConnectedCount => _file.Connectors.Count(c => c.Connected);
}
