using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using LunaChat.Models;

namespace LunaChat.Services;

/// <summary>
/// Talks to one family of model APIs: verifies credentials and streams a
/// completion as text deltas. One implementation per <see cref="ProviderKind"/>.
/// </summary>
public interface IChatModelClient
{
    Task<VerifyResult> VerifyAsync(ProviderCredentials creds, CancellationToken ct);

    IAsyncEnumerable<string> StreamAsync(
        ProviderCredentials creds, string model, string? system,
        IReadOnlyList<ChatTurn> turns, CancellationToken ct);
}

public static class ModelClientFactory
{
    // One HttpClient for the process; per-request timeout is handled by the
    // CancellationToken so streaming isn't cut off by a global timeout.
    internal static readonly HttpClient Http = new() { Timeout = Timeout.InfiniteTimeSpan };

    public static IChatModelClient Create(ProviderKind kind) => kind switch
    {
        ProviderKind.Anthropic => new AnthropicClient(),
        ProviderKind.Gemini => new GeminiClient(),
        _ => new OpenAiCompatibleClient()
    };

    /// <summary>Read an SSE stream, yielding each `data:` payload (minus the prefix).</summary>
    internal static async IAsyncEnumerable<string> ReadSseAsync(
        HttpResponseMessage resp, [EnumeratorCancellation] CancellationToken ct)
    {
        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(ct);
            if (line == null) break;
            if (!line.StartsWith("data:")) continue;
            var payload = line.Substring(5).TrimStart();
            if (payload.Length == 0) continue;
            yield return payload;
        }
    }

    internal static async Task<string> ErrorTextAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        var body = "";
        try { body = await resp.Content.ReadAsStringAsync(ct); } catch { /* ignore */ }
        var status = (int)resp.StatusCode;
        // Pull a human message out of the common {"error":{"message":...}} shape.
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var err))
            {
                if (err.ValueKind == JsonValueKind.Object && err.TryGetProperty("message", out var m))
                    return $"{status} · {m.GetString()}";
                if (err.ValueKind == JsonValueKind.String)
                    return $"{status} · {err.GetString()}";
            }
        }
        catch { /* not json */ }
        return status switch
        {
            401 or 403 => $"{status} · invalid or unauthorized key",
            404 => $"{status} · endpoint or model not found",
            _ => $"{status} · request failed"
        };
    }
}

/// <summary>OpenAI Chat Completions shape — covers OpenAI, DeepSeek, Mistral, xAI,
/// Together, Fireworks, GLM, Kimi, Qwen, MiniMax, and local Ollama.</summary>
public sealed class OpenAiCompatibleClient : IChatModelClient
{
    public async Task<VerifyResult> VerifyAsync(ProviderCredentials creds, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{creds.BaseUrl}/models");
            if (!string.IsNullOrEmpty(creds.ApiKey))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", creds.ApiKey);
            using var resp = await ModelClientFactory.Http.SendAsync(req, ct);
            if (resp.IsSuccessStatusCode) return VerifyResult.Success;
            return VerifyResult.Fail(await ModelClientFactory.ErrorTextAsync(resp, ct));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return VerifyResult.Fail(Friendly(ex)); }
    }

    public async IAsyncEnumerable<string> StreamAsync(
        ProviderCredentials creds, string model, string? system,
        IReadOnlyList<ChatTurn> turns, [EnumeratorCancellation] CancellationToken ct)
    {
        var messages = new List<object>();
        if (!string.IsNullOrWhiteSpace(system))
            messages.Add(new { role = "system", content = system });
        foreach (var t in turns)
            messages.Add(new { role = t.Role, content = t.Content });

        var body = JsonSerializer.Serialize(new { model, messages, stream = true });
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{creds.BaseUrl}/chat/completions")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        if (!string.IsNullOrEmpty(creds.ApiKey))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", creds.ApiKey);

        using var resp = await ModelClientFactory.Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!resp.IsSuccessStatusCode)
            throw new ModelClientException(await ModelClientFactory.ErrorTextAsync(resp, ct));

        await foreach (var payload in ModelClientFactory.ReadSseAsync(resp, ct))
        {
            if (payload == "[DONE]") yield break;
            string? delta = null;
            try
            {
                using var doc = JsonDocument.Parse(payload);
                var choices = doc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() == 0) continue;
                var d = choices[0].GetProperty("delta");
                if (d.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String)
                    delta = c.GetString();
            }
            catch { continue; }
            if (!string.IsNullOrEmpty(delta)) yield return delta!;
        }
    }

    private static string Friendly(Exception ex) =>
        ex is HttpRequestException ? "couldn't reach the endpoint" : ex.Message;
}

/// <summary>Anthropic Messages API.</summary>
public sealed class AnthropicClient : IChatModelClient
{
    private const string Version = "2023-06-01";

    public async Task<VerifyResult> VerifyAsync(ProviderCredentials creds, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{creds.BaseUrl}/v1/models");
            req.Headers.Add("x-api-key", creds.ApiKey);
            req.Headers.Add("anthropic-version", Version);
            using var resp = await ModelClientFactory.Http.SendAsync(req, ct);
            if (resp.IsSuccessStatusCode) return VerifyResult.Success;
            return VerifyResult.Fail(await ModelClientFactory.ErrorTextAsync(resp, ct));
        }
        catch (OperationCanceledException) { throw; }
        catch (HttpRequestException) { return VerifyResult.Fail("couldn't reach the endpoint"); }
        catch (Exception ex) { return VerifyResult.Fail(ex.Message); }
    }

    public async IAsyncEnumerable<string> StreamAsync(
        ProviderCredentials creds, string model, string? system,
        IReadOnlyList<ChatTurn> turns, [EnumeratorCancellation] CancellationToken ct)
    {
        // Anthropic keeps the system prompt out of the messages array.
        var messages = turns
            .Where(t => t.Role is "user" or "assistant")
            .Select(t => new { role = t.Role, content = t.Content })
            .ToList();

        var payloadObj = new Dictionary<string, object?>
        {
            ["model"] = model,
            ["max_tokens"] = 4096,
            ["messages"] = messages,
            ["stream"] = true
        };
        if (!string.IsNullOrWhiteSpace(system)) payloadObj["system"] = system;

        var body = JsonSerializer.Serialize(payloadObj);
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{creds.BaseUrl}/v1/messages")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        req.Headers.Add("x-api-key", creds.ApiKey);
        req.Headers.Add("anthropic-version", Version);

        using var resp = await ModelClientFactory.Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!resp.IsSuccessStatusCode)
            throw new ModelClientException(await ModelClientFactory.ErrorTextAsync(resp, ct));

        await foreach (var payload in ModelClientFactory.ReadSseAsync(resp, ct))
        {
            string? delta = null;
            try
            {
                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;
                if (root.TryGetProperty("type", out var type) &&
                    type.GetString() == "content_block_delta" &&
                    root.TryGetProperty("delta", out var d) &&
                    d.TryGetProperty("text", out var txt))
                    delta = txt.GetString();
            }
            catch { continue; }
            if (!string.IsNullOrEmpty(delta)) yield return delta!;
        }
    }
}

/// <summary>Google Gemini generateContent (SSE) API.</summary>
public sealed class GeminiClient : IChatModelClient
{
    public async Task<VerifyResult> VerifyAsync(ProviderCredentials creds, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{creds.BaseUrl}/models?key={creds.ApiKey}");
            using var resp = await ModelClientFactory.Http.SendAsync(req, ct);
            if (resp.IsSuccessStatusCode) return VerifyResult.Success;
            return VerifyResult.Fail(await ModelClientFactory.ErrorTextAsync(resp, ct));
        }
        catch (OperationCanceledException) { throw; }
        catch (HttpRequestException) { return VerifyResult.Fail("couldn't reach the endpoint"); }
        catch (Exception ex) { return VerifyResult.Fail(ex.Message); }
    }

    public async IAsyncEnumerable<string> StreamAsync(
        ProviderCredentials creds, string model, string? system,
        IReadOnlyList<ChatTurn> turns, [EnumeratorCancellation] CancellationToken ct)
    {
        var contents = turns
            .Where(t => t.Role is "user" or "assistant")
            .Select(t => new
            {
                role = t.Role == "assistant" ? "model" : "user",
                parts = new[] { new { text = t.Content } }
            })
            .ToList();

        var payloadObj = new Dictionary<string, object?>
        {
            ["contents"] = contents
        };
        if (!string.IsNullOrWhiteSpace(system))
            payloadObj["systemInstruction"] = new { parts = new[] { new { text = system } } };

        var body = JsonSerializer.Serialize(payloadObj);
        var url = $"{creds.BaseUrl}/models/{model}:streamGenerateContent?alt=sse&key={creds.ApiKey}";
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        using var resp = await ModelClientFactory.Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!resp.IsSuccessStatusCode)
            throw new ModelClientException(await ModelClientFactory.ErrorTextAsync(resp, ct));

        await foreach (var payload in ModelClientFactory.ReadSseAsync(resp, ct))
        {
            string? delta = null;
            try
            {
                using var doc = JsonDocument.Parse(payload);
                var candidates = doc.RootElement.GetProperty("candidates");
                if (candidates.GetArrayLength() == 0) continue;
                var parts = candidates[0].GetProperty("content").GetProperty("parts");
                var sb = new StringBuilder();
                foreach (var part in parts.EnumerateArray())
                    if (part.TryGetProperty("text", out var t)) sb.Append(t.GetString());
                delta = sb.ToString();
            }
            catch { continue; }
            if (!string.IsNullOrEmpty(delta)) yield return delta!;
        }
    }
}

/// <summary>Raised when a provider returns a non-success status during streaming.</summary>
public sealed class ModelClientException : Exception
{
    public ModelClientException(string message) : base(message) { }
}
