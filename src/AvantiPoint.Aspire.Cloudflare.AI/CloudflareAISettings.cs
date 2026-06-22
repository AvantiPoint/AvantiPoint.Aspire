using System.Data.Common;

namespace AvantiPoint.Aspire.Cloudflare.AI;

/// <summary>
/// Settings for the Cloudflare AI client integration. Values are typically supplied via the
/// Aspire-injected connection string (an OpenAI-compatible <c>Endpoint</c> plus <c>Key</c> and the
/// <c>ChatModel</c>/<c>EmbeddingModel</c>) but can be overridden in code.
/// </summary>
public sealed class CloudflareAISettings
{
    /// <summary>The full connection string. When set, its components populate the other properties.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>The backend provider: <c>Ollama</c>, <c>WorkersAI</c>, or <c>AIGateway</c>.</summary>
    public string? Provider { get; set; }

    /// <summary>The OpenAI-compatible base endpoint (e.g. <c>.../ai/v1</c> or <c>http://localhost:11434/v1</c>).</summary>
    public string? Endpoint { get; set; }

    /// <summary>The API token (empty for the local Ollama emulator).</summary>
    public string? Key { get; set; }

    /// <summary>The chat model id.</summary>
    public string? ChatModel { get; set; }

    /// <summary>The embedding model id.</summary>
    public string? EmbeddingModel { get; set; }

    internal void ApplyConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        ConnectionString = connectionString;

        var parsed = new DbConnectionStringBuilder { ConnectionString = connectionString };

        if (TryGet(parsed, "Provider", out var provider))
        {
            Provider = provider;
        }

        if (TryGet(parsed, "Endpoint", out var endpoint))
        {
            Endpoint = endpoint;
        }

        if (TryGet(parsed, "Key", out var key))
        {
            Key = key;
        }

        if (TryGet(parsed, "ChatModel", out var chatModel))
        {
            ChatModel = chatModel;
        }

        if (TryGet(parsed, "EmbeddingModel", out var embeddingModel))
        {
            EmbeddingModel = embeddingModel;
        }
    }

    private static bool TryGet(DbConnectionStringBuilder builder, string key, out string value)
    {
        if (builder.TryGetValue(key, out var raw) && raw is not null)
        {
            value = raw.ToString() ?? string.Empty;
            return !string.IsNullOrEmpty(value);
        }

        value = string.Empty;
        return false;
    }
}
