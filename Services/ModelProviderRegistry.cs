using LunaChat.Models;

namespace LunaChat.Services;

/// <summary>
/// The built-in catalogue of model providers luna-chat can talk to. Modeled on
/// OpenWorker's provider gallery. Curated model lists are a starting point — the
/// user can always type any model id the provider accepts.
/// </summary>
public static class ModelProviderRegistry
{
    private static ProviderField Key(string help) => new()
    {
        Key = "api_key", Label = "API key", Secret = true, Placeholder = "sk-…", Help = help
    };

    private static ProviderField BaseUrl(string def) => new()
    {
        Key = "base_url", Label = "Custom endpoint", Placeholder = def, Default = def,
        Help = "Override the default API base URL (advanced)."
    };

    public static readonly IReadOnlyList<ModelProviderDef> All = new List<ModelProviderDef>
    {
        new()
        {
            Id = "anthropic", Title = "Anthropic", Kind = ProviderKind.Anthropic,
            Blurb = "Claude models — strong tool-use and long-context work.",
            DefaultBaseUrl = "https://api.anthropic.com",
            KeyHelpUrl = "https://console.anthropic.com/settings/keys", KeyHelpLabel = "console.anthropic.com",
            Fields = { Key("Your Anthropic API key."), BaseUrl("https://api.anthropic.com") },
            Models =
            {
                new("claude-opus-4-8", "Claude Opus 4.8"),
                new("claude-sonnet-5", "Claude Sonnet 5"),
                new("claude-haiku-4-5-20251001", "Claude Haiku 4.5"),
            }
        },
        new()
        {
            Id = "openai", Title = "OpenAI", Kind = ProviderKind.OpenAiCompatible,
            Blurb = "GPT models via the OpenAI API.",
            DefaultBaseUrl = "https://api.openai.com/v1",
            KeyHelpUrl = "https://platform.openai.com/api-keys", KeyHelpLabel = "platform.openai.com",
            Fields = { Key("Your OpenAI API key."), BaseUrl("https://api.openai.com/v1") },
            Models =
            {
                new("gpt-4o", "GPT-4o"),
                new("gpt-4o-mini", "GPT-4o mini"),
                new("gpt-4.1", "GPT-4.1"),
                new("o3", "o3"),
            }
        },
        new()
        {
            Id = "gemini", Title = "Google Gemini", Kind = ProviderKind.Gemini,
            Blurb = "Gemini models via Google AI Studio.",
            DefaultBaseUrl = "https://generativelanguage.googleapis.com/v1beta",
            KeyHelpUrl = "https://aistudio.google.com/apikey", KeyHelpLabel = "aistudio.google.com",
            Fields = { Key("Your Google AI Studio API key."), BaseUrl("https://generativelanguage.googleapis.com/v1beta") },
            Models =
            {
                new("gemini-2.5-pro", "Gemini 2.5 Pro"),
                new("gemini-2.5-flash", "Gemini 2.5 Flash"),
            }
        },
        new()
        {
            Id = "deepseek", Title = "DeepSeek", Kind = ProviderKind.OpenAiCompatible,
            Blurb = "DeepSeek chat and reasoning models.",
            DefaultBaseUrl = "https://api.deepseek.com/v1",
            KeyHelpUrl = "https://platform.deepseek.com/api_keys", KeyHelpLabel = "platform.deepseek.com",
            Fields = { Key("Your DeepSeek API key."), BaseUrl("https://api.deepseek.com/v1") },
            Models = { new("deepseek-chat", "DeepSeek Chat"), new("deepseek-reasoner", "DeepSeek Reasoner") }
        },
        new()
        {
            Id = "mistral", Title = "Mistral", Kind = ProviderKind.OpenAiCompatible,
            Blurb = "Mistral open and commercial models.",
            DefaultBaseUrl = "https://api.mistral.ai/v1",
            KeyHelpUrl = "https://console.mistral.ai/api-keys", KeyHelpLabel = "console.mistral.ai",
            Fields = { Key("Your Mistral API key."), BaseUrl("https://api.mistral.ai/v1") },
            Models = { new("mistral-large-latest", "Mistral Large"), new("mistral-small-latest", "Mistral Small") }
        },
        new()
        {
            Id = "xai", Title = "xAI (Grok)", Kind = ProviderKind.OpenAiCompatible,
            Blurb = "Grok models from xAI.",
            DefaultBaseUrl = "https://api.x.ai/v1",
            KeyHelpUrl = "https://console.x.ai", KeyHelpLabel = "console.x.ai",
            Fields = { Key("Your xAI API key."), BaseUrl("https://api.x.ai/v1") },
            Models = { new("grok-4", "Grok 4"), new("grok-3", "Grok 3") }
        },
        new()
        {
            Id = "together", Title = "Together", Kind = ProviderKind.OpenAiCompatible,
            Blurb = "Open-weight models hosted on Together.",
            DefaultBaseUrl = "https://api.together.xyz/v1",
            KeyHelpUrl = "https://api.together.xyz/settings/api-keys", KeyHelpLabel = "together.xyz",
            Fields = { Key("Your Together API key."), BaseUrl("https://api.together.xyz/v1") },
            Models = { new("meta-llama/Llama-3.3-70B-Instruct-Turbo", "Llama 3.3 70B") }
        },
        new()
        {
            Id = "fireworks", Title = "Fireworks", Kind = ProviderKind.OpenAiCompatible,
            Blurb = "Open-weight models hosted on Fireworks.",
            DefaultBaseUrl = "https://api.fireworks.ai/inference/v1",
            KeyHelpUrl = "https://fireworks.ai/account/api-keys", KeyHelpLabel = "fireworks.ai",
            Fields = { Key("Your Fireworks API key."), BaseUrl("https://api.fireworks.ai/inference/v1") },
            Models = { new("accounts/fireworks/models/llama-v3p3-70b-instruct", "Llama 3.3 70B") }
        },
        new()
        {
            Id = "zai", Title = "GLM (Z.ai)", Kind = ProviderKind.OpenAiCompatible,
            Blurb = "GLM models from Z.ai.",
            DefaultBaseUrl = "https://api.z.ai/api/paas/v4",
            KeyHelpUrl = "https://z.ai/manage-apikey/apikey-list", KeyHelpLabel = "z.ai",
            Fields = { Key("Your Z.ai API key."), BaseUrl("https://api.z.ai/api/paas/v4") },
            Models = { new("glm-4.6", "GLM-4.6") }
        },
        new()
        {
            Id = "kimi", Title = "Kimi (Moonshot)", Kind = ProviderKind.OpenAiCompatible,
            Blurb = "Moonshot Kimi models.",
            DefaultBaseUrl = "https://api.moonshot.ai/v1",
            KeyHelpUrl = "https://platform.moonshot.ai/console/api-keys", KeyHelpLabel = "platform.moonshot.ai",
            Fields = { Key("Your Moonshot API key."), BaseUrl("https://api.moonshot.ai/v1") },
            Models = { new("kimi-k2-0711-preview", "Kimi K2"), new("moonshot-v1-8k", "Moonshot v1 8k") }
        },
        new()
        {
            Id = "qwen", Title = "Qwen", Kind = ProviderKind.OpenAiCompatible,
            Blurb = "Alibaba Qwen models (DashScope compatible mode).",
            DefaultBaseUrl = "https://dashscope-intl.aliyuncs.com/compatible-mode/v1",
            KeyHelpUrl = "https://modelstudio.console.alibabacloud.com", KeyHelpLabel = "alibabacloud.com",
            Fields = { Key("Your DashScope API key."), BaseUrl("https://dashscope-intl.aliyuncs.com/compatible-mode/v1") },
            Models = { new("qwen-max", "Qwen Max"), new("qwen-plus", "Qwen Plus") }
        },
        new()
        {
            Id = "minimax", Title = "MiniMax", Kind = ProviderKind.OpenAiCompatible,
            Blurb = "MiniMax text models.",
            DefaultBaseUrl = "https://api.minimax.io/v1",
            KeyHelpUrl = "https://platform.minimax.io", KeyHelpLabel = "platform.minimax.io",
            Fields = { Key("Your MiniMax API key."), BaseUrl("https://api.minimax.io/v1") },
            Models = { new("MiniMax-Text-01", "MiniMax Text 01") }
        },
        new()
        {
            Id = "ollama", Title = "Ollama", Kind = ProviderKind.OpenAiCompatible, NeedsKey = false,
            Blurb = "Run open-weight models locally — no API key needed.",
            DefaultBaseUrl = "http://localhost:11434/v1",
            KeyHelpUrl = "https://ollama.com/download", KeyHelpLabel = "ollama.com",
            Fields = { BaseUrl("http://localhost:11434/v1") },
            Models = { new("llama3.1", "Llama 3.1"), new("qwen2.5", "Qwen 2.5") }
        },
    };

    public static ModelProviderDef? Find(string id) => All.FirstOrDefault(p => p.Id == id);
}
