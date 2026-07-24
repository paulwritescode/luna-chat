using System.Runtime.CompilerServices;
using System.Threading;
using LunaChat.Models;

namespace LunaChat.Services;

/// <summary>
/// High-level model access: verify (test) credentials and stream chat completions
/// for the currently selected provider/model. Sits on top of <see cref="ProviderStore"/>
/// and the per-kind <see cref="IChatModelClient"/> implementations.
/// </summary>
public class ModelChatService
{
    private readonly ProviderStore _store;

    public ModelChatService(ProviderStore store) => _store = store;

    /// <summary>Verify credentials as typed in the key form (nothing persisted yet).</summary>
    public async Task<VerifyResult> VerifyAsync(
        ModelProviderDef def, IReadOnlyDictionary<string, string> fields, CancellationToken ct)
    {
        var baseUrl = fields.TryGetValue("base_url", out var b) && !string.IsNullOrWhiteSpace(b)
            ? b : def.DefaultBaseUrl;
        var apiKey = "";
        if (def.NeedsKey && def.SecretFieldKey != null)
            fields.TryGetValue(def.SecretFieldKey, out apiKey);

        var creds = new ProviderCredentials
        {
            BaseUrl = (baseUrl ?? "").TrimEnd('/'),
            ApiKey = apiKey ?? ""
        };
        if (def.NeedsKey && string.IsNullOrWhiteSpace(creds.ApiKey))
            return VerifyResult.Fail("enter an API key first");

        var client = ModelClientFactory.Create(def.Kind);
        return await client.VerifyAsync(creds, ct);
    }

    /// <summary>Stream a completion for a provider/model using stored credentials.</summary>
    public async IAsyncEnumerable<string> StreamAsync(
        string providerId, string model, string? system,
        IReadOnlyList<ChatTurn> turns, [EnumeratorCancellation] CancellationToken ct)
    {
        var def = ModelProviderRegistry.Find(providerId)
            ?? throw new ModelClientException($"unknown provider '{providerId}'");
        var creds = await _store.GetCredentialsAsync(def);
        if (def.NeedsKey && string.IsNullOrEmpty(creds.ApiKey))
            throw new ModelClientException("no API key stored for this provider — add it in Settings ▸ Models");

        var client = ModelClientFactory.Create(def.Kind);
        await foreach (var delta in client.StreamAsync(creds, model, system, turns, ct))
            yield return delta;
    }
}
