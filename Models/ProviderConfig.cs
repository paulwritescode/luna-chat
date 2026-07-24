namespace LunaChat.Models;

/// <summary>
/// Per-provider NON-SECRET config, persisted to providers.json. The API key
/// itself lives in the OS credential vault — only <see cref="HasKey"/> records
/// that one was stored, so the UI can show "Connected" without reading the vault.
/// </summary>
public class ProviderConfig
{
    public string Id { get; set; } = "";
    public Dictionary<string, string> Values { get; set; } = new();
    public bool HasKey { get; set; }
    public long LastVerifiedUnix { get; set; }
}

/// <summary>Root document for providers.json.</summary>
public class ProvidersFile
{
    public List<ProviderConfig> Providers { get; set; } = new();
}

/// <summary>Resolved credentials handed to a model client at call time.</summary>
public class ProviderCredentials
{
    public string ApiKey { get; set; } = "";
    public string BaseUrl { get; set; } = "";
}
