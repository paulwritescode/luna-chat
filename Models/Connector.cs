namespace LunaChat.Models;

/// <summary>
/// Static definition of a connector (Slack, GitHub, …). Credentials are supplied
/// manually (API token / key) and stored in the OS vault, mirroring OpenWorker's
/// "use connectors via manually-created credentials" local mode.
/// </summary>
public class ConnectorDef
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public bool TwoWay { get; init; }

    /// <summary>Brand accent (hex) — used for the icon initial.</summary>
    public string BrandColor { get; init; } = "#6B7280";

    /// <summary>Soft brand plate (hex) behind the icon initial.</summary>
    public string BrandSoft { get; init; } = "#EEF0F2";

    public string HelpUrl { get; init; } = "";
    public string HelpLabel { get; init; } = "";

    public List<ProviderField> Fields { get; init; } = new();

    public string? SecretFieldKey => Fields.FirstOrDefault(f => f.Secret)?.Key;
    public string Initial => string.IsNullOrEmpty(Title) ? "?" : Title[..1];
}

/// <summary>Persisted per-connector NON-SECRET state (secret lives in the vault).</summary>
public class ConnectorState
{
    public string Id { get; set; } = "";
    public Dictionary<string, string> Values { get; set; } = new();
    public bool Connected { get; set; }
}

public class ConnectorsFile
{
    public List<ConnectorState> Connectors { get; set; } = new();
}
