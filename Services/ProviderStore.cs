using System.IO;
using System.Text.Json;
using LunaChat.Models;

namespace LunaChat.Services;

/// <summary>
/// Owns provider configuration: non-secret values in providers.json, API keys in
/// the OS credential vault (<see cref="ISecretStore"/>). Single entry point the
/// Models UI and the chat pipeline use.
/// </summary>
public class ProviderStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ISecretStore _secrets;
    private ProvidersFile _file;

    public ProviderStore(ISecretStore? secrets = null)
    {
        _secrets = secrets ?? SecretStoreFactory.Instance;
        _file = Load();
    }

    public string BackendName => _secrets.BackendName;

    private static string FilePath => Path.Combine(PlatformDirs.DataDir, "providers.json");

    private static ProvidersFile Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new ProvidersFile();
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<ProvidersFile>(json, JsonOptions) ?? new ProvidersFile();
        }
        catch { return new ProvidersFile(); }
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

    private ProviderConfig? ConfigFor(string id) => _file.Providers.FirstOrDefault(p => p.Id == id);

    /// <summary>True once a provider has a stored key (or a keyless provider verified this device).</summary>
    public bool IsConfigured(string id)
    {
        var def = ModelProviderRegistry.Find(id);
        var cfg = ConfigFor(id);
        if (def is { NeedsKey: false }) return cfg is { LastVerifiedUnix: > 0 };
        return cfg is { HasKey: true };
    }

    public long LastVerified(string id) => ConfigFor(id)?.LastVerifiedUnix ?? 0;

    /// <summary>Non-secret field value (e.g. custom base_url), or the def default.</summary>
    public string ValueOr(string id, string key, string fallback)
    {
        var cfg = ConfigFor(id);
        if (cfg != null && cfg.Values.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v))
            return v;
        return fallback;
    }

    /// <summary>Resolve base_url + api_key for a live call.</summary>
    public async Task<ProviderCredentials> GetCredentialsAsync(ModelProviderDef def)
    {
        var baseUrl = ValueOr(def.Id, "base_url", def.DefaultBaseUrl).TrimEnd('/');
        var key = "";
        if (def.NeedsKey)
            key = await _secrets.GetAsync(def.Id) ?? "";
        return new ProviderCredentials { ApiKey = key, BaseUrl = baseUrl };
    }

    /// <summary>Split incoming fields: secret → vault, the rest → providers.json.</summary>
    public async Task SaveAsync(ModelProviderDef def, IReadOnlyDictionary<string, string> fields)
    {
        var cfg = ConfigFor(def.Id);
        if (cfg == null)
        {
            cfg = new ProviderConfig { Id = def.Id };
            _file.Providers.Add(cfg);
        }

        foreach (var f in def.Fields)
        {
            if (!fields.TryGetValue(f.Key, out var value)) continue;
            value = value?.Trim() ?? "";
            if (f.Secret)
            {
                if (value.Length > 0)
                {
                    await _secrets.SetAsync(def.Id, value);
                    cfg.HasKey = true;
                }
            }
            else
            {
                cfg.Values[f.Key] = value;
            }
        }

        await PersistAsync();
    }

    /// <summary>Record a successful verification (drives the "Connected" state).</summary>
    public async Task MarkVerifiedAsync(string id)
    {
        var cfg = ConfigFor(id);
        if (cfg == null)
        {
            cfg = new ProviderConfig { Id = id };
            _file.Providers.Add(cfg);
        }
        cfg.LastVerifiedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await PersistAsync();
    }

    /// <summary>Forget a provider: drop its key from the vault and its config row.</summary>
    public async Task RemoveAsync(string id)
    {
        try { await _secrets.DeleteAsync(id); } catch { /* ignore */ }
        _file.Providers.RemoveAll(p => p.Id == id);
        await PersistAsync();
    }
}
