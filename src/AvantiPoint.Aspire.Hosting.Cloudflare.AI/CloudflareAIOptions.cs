using Aspire.Hosting;
namespace AvantiPoint.Aspire.Hosting.Cloudflare.AI;

/// <summary>
/// Configures the models and routing for a <see cref="CloudflareAIResource"/>. The same logical
/// resource maps to local Ollama models under <c>RunAsEmulator()</c> and to Cloudflare models in
/// production, so application code only references the connection — never a specific backend.
/// </summary>
[AspireExport(ExposeProperties = true)]
public sealed class CloudflareAIOptions
{
    /// <summary>Default Cloudflare Workers AI chat model.</summary>
    public const string DefaultCloudflareChatModel = "@cf/meta/llama-3.1-8b-instruct";

    /// <summary>Default Cloudflare Workers AI embedding model.</summary>
    public const string DefaultCloudflareEmbeddingModel = "@cf/baai/bge-base-en-v1.5";

    /// <summary>Default local (Ollama) chat model used under <c>RunAsEmulator()</c>.</summary>
    public const string DefaultLocalChatModel = "llama3.2";

    /// <summary>Default local (Ollama) embedding model used under <c>RunAsEmulator()</c>.</summary>
    public const string DefaultLocalEmbeddingModel = "all-minilm";

    /// <summary>
    /// The chat model used against Cloudflare. A Workers AI model id (e.g. <c>@cf/meta/llama-3.1-8b-instruct</c>),
    /// or — when <see cref="GatewayId"/> is set — a provider-prefixed model (e.g. <c>anthropic/claude-3-5-sonnet</c>,
    /// <c>openai/gpt-4o</c>). Defaults to <see cref="DefaultCloudflareChatModel"/>.
    /// </summary>
    public string? ChatModel { get; set; }

    /// <summary>The embedding model used against Cloudflare. Defaults to <see cref="DefaultCloudflareEmbeddingModel"/>.</summary>
    public string? EmbeddingModel { get; set; }

    /// <summary>The local Ollama chat model pulled and served under <c>RunAsEmulator()</c>.</summary>
    public string LocalChatModel { get; set; } = DefaultLocalChatModel;

    /// <summary>The local Ollama embedding model pulled and served under <c>RunAsEmulator()</c>.</summary>
    public string LocalEmbeddingModel { get; set; } = DefaultLocalEmbeddingModel;

    /// <summary>
    /// Optional AI Gateway id. When set, the real endpoint routes through the gateway's OpenAI-compatible
    /// (<c>/compat</c>) URL — enabling caching, logging, rate limiting and multi-provider models — and the
    /// gateway is provisioned at deploy. When unset, requests go directly to Workers AI.
    /// </summary>
    public string? GatewayId { get; set; }
}
