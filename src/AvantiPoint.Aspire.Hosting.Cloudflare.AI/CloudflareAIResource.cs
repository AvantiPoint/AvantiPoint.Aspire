using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.AI;

/// <summary>
/// A Cloudflare AI resource modeled as an Aspire resource. Exposes a connection string describing an
/// OpenAI-compatible endpoint plus the chat/embedding models to use. By default it targets Cloudflare —
/// Workers AI, or an AI Gateway when one is configured; <see cref="CloudflareAIExtensions.RunAsEmulator"/>
/// points it at a local Ollama server during <c>aspire run</c>. The client integration turns the
/// connection string into a <c>Microsoft.Extensions.AI</c> <c>IChatClient</c>/<c>IEmbeddingGenerator</c>.
/// </summary>
public sealed class CloudflareAIResource : Resource, IResourceWithConnectionString, IResourceWithWaitSupport, ICloudflareResource
{
    internal CloudflareAIResource(string name, CloudflareEnvironmentResource environment, CloudflareAIOptions options)
        : base(name)
    {
        Environment = environment;
        Options = options;
    }

    /// <summary>The Cloudflare environment that owns this AI resource.</summary>
    public CloudflareEnvironmentResource Environment { get; }

    /// <summary>The configured models and gateway.</summary>
    public CloudflareAIOptions Options { get; }

    /// <summary>True when this resource is served by a local Ollama server (opt-in via <c>RunAsEmulator()</c>).</summary>
    public bool UseEmulator { get; internal set; }

    // Emulator wiring (set when UseEmulator is true).
    internal EndpointReference? EmulatorEndpoint { get; set; }

    // Optional token override for runtime access; defaults to the environment API token.
    internal ParameterResource? AccessToken { get; set; }

    /// <inheritdoc />
    public ReferenceExpression ConnectionStringExpression => BuildConnectionString();

    private ReferenceExpression BuildConnectionString()
    {
        if (UseEmulator)
        {
            var endpoint = EmulatorEndpoint
                ?? throw new InvalidOperationException($"Ollama emulator endpoint for AI resource '{Name}' has not been wired.");

            return ReferenceExpression.Create(
                $"Provider=Ollama;Endpoint=http://{endpoint.Property(EndpointProperty.Host)}:{endpoint.Property(EndpointProperty.Port)}/v1;ChatModel={Options.LocalChatModel};EmbeddingModel={Options.LocalEmbeddingModel}");
        }

        var accountId = Environment.AccountId;
        var token = AccessToken ?? Environment.ApiToken;
        var chatModel = Options.ChatModel ?? CloudflareAIOptions.DefaultCloudflareChatModel;
        var embeddingModel = Options.EmbeddingModel ?? CloudflareAIOptions.DefaultCloudflareEmbeddingModel;

        if (Options.GatewayId is { Length: > 0 } gatewayId)
        {
            // The AI Gateway /compat endpoint routes by provider-prefixed model ids, so bare Workers AI
            // ids (@cf/...) must be prefixed with "workers-ai/". Already-prefixed ids (anthropic/..., etc.)
            // are left untouched.
            var gatewayChatModel = ToGatewayModel(chatModel);
            var gatewayEmbeddingModel = ToGatewayModel(embeddingModel);
            return ReferenceExpression.Create(
                $"Provider=AIGateway;Endpoint=https://gateway.ai.cloudflare.com/v1/{accountId}/{gatewayId}/compat;Key={token};ChatModel={gatewayChatModel};EmbeddingModel={gatewayEmbeddingModel}");
        }

        return ReferenceExpression.Create(
            $"Provider=WorkersAI;Endpoint=https://api.cloudflare.com/client/v4/accounts/{accountId}/ai/v1;Key={token};ChatModel={chatModel};EmbeddingModel={embeddingModel}");
    }

    // AI Gateway's OpenAI-compatible endpoint routes by provider-prefixed model id. Bare Workers AI ids
    // (@cf/...) are prefixed with "workers-ai/"; ids that already carry a provider prefix are unchanged.
    private static string ToGatewayModel(string model)
        => model.StartsWith("@cf/", StringComparison.OrdinalIgnoreCase) ? $"workers-ai/{model}" : model;
}
