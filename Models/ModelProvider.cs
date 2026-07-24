namespace LunaChat.Models;

/// <summary>Wire protocol a provider speaks (selects which client drives it).</summary>
public enum ProviderKind
{
    OpenAiCompatible,
    Anthropic,
    Gemini
}

/// <summary>One credential/config input on a provider's key form.</summary>
public class ProviderField
{
    public string Key { get; init; } = "";
    public string Label { get; init; } = "";
    public bool Secret { get; init; }
    public string Placeholder { get; init; } = "";
    public string Default { get; init; } = "";
    public string Help { get; init; } = "";
}

/// <summary>A curated model id the user can pick for a provider.</summary>
public class CuratedModel
{
    public string Id { get; init; } = "";
    public string Label { get; init; } = "";

    public CuratedModel() { }
    public CuratedModel(string id, string? label = null) { Id = id; Label = label ?? id; }
}

/// <summary>
/// Static definition of a model provider: how to reach it, what it needs,
/// and a curated model list. Mirrors OpenWorker's provider registry.
/// </summary>
public class ModelProviderDef
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string Blurb { get; init; } = "";
    public ProviderKind Kind { get; init; } = ProviderKind.OpenAiCompatible;

    /// <summary>False for local providers like Ollama that need no API key.</summary>
    public bool NeedsKey { get; init; } = true;

    /// <summary>Base URL used when the user doesn't override it.</summary>
    public string DefaultBaseUrl { get; init; } = "";

    public string KeyHelpUrl { get; init; } = "";
    public string KeyHelpLabel { get; init; } = "";

    public List<ProviderField> Fields { get; init; } = new();
    public List<CuratedModel> Models { get; init; } = new();

    /// <summary>The single secret field's key (null for keyless providers).</summary>
    public string? SecretFieldKey => Fields.FirstOrDefault(f => f.Secret)?.Key;
}
