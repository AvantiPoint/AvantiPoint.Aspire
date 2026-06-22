using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI;

namespace AvantiPoint.Aspire.Cloudflare.AI;

/// <summary>
/// Registers <c>Microsoft.Extensions.AI</c> clients (<see cref="IChatClient"/> and
/// <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/>) from an Aspire-injected Cloudflare AI connection
/// string. The endpoint and models are OpenAI-compatible, so the same registration targets a local Ollama
/// server during development and Cloudflare Workers AI / AI Gateway in production.
/// </summary>
public static class CloudflareAIClientExtensions
{
    private const string DefaultConfigSectionRoot = "Aspire:Cloudflare:AI";

    /// <summary>Registers a singleton <see cref="IChatClient"/> for the given connection name.</summary>
    public static void AddCloudflareAIChatClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<CloudflareAISettings>? configureSettings = null)
        => builder.AddChatClientInternal(connectionName, serviceKey: null, configureSettings);

    /// <summary>Registers a keyed <see cref="IChatClient"/> (keyed by <paramref name="name"/>).</summary>
    public static void AddKeyedCloudflareAIChatClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<CloudflareAISettings>? configureSettings = null)
        => builder.AddChatClientInternal(name, serviceKey: name, configureSettings);

    /// <summary>Registers a singleton <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> for the given connection name.</summary>
    public static void AddCloudflareAIEmbeddingGenerator(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<CloudflareAISettings>? configureSettings = null)
        => builder.AddEmbeddingGeneratorInternal(connectionName, serviceKey: null, configureSettings);

    /// <summary>Registers a keyed <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> (keyed by <paramref name="name"/>).</summary>
    public static void AddKeyedCloudflareAIEmbeddingGenerator(
        this IHostApplicationBuilder builder,
        string name,
        Action<CloudflareAISettings>? configureSettings = null)
        => builder.AddEmbeddingGeneratorInternal(name, serviceKey: name, configureSettings);

    private static void AddChatClientInternal(
        this IHostApplicationBuilder builder,
        string connectionName,
        string? serviceKey,
        Action<CloudflareAISettings>? configureSettings)
    {
        var settings = ResolveSettings(builder, connectionName, configureSettings);
        var client = CreateOpenAIClient(settings, connectionName);
        var model = RequireModel(settings.ChatModel, connectionName, "chat");

        IChatClient chatClient = client.GetChatClient(model).AsIChatClient();

        if (serviceKey is null)
        {
            builder.Services.AddSingleton(chatClient);
        }
        else
        {
            builder.Services.AddKeyedSingleton(serviceKey, chatClient);
        }
    }

    private static void AddEmbeddingGeneratorInternal(
        this IHostApplicationBuilder builder,
        string connectionName,
        string? serviceKey,
        Action<CloudflareAISettings>? configureSettings)
    {
        var settings = ResolveSettings(builder, connectionName, configureSettings);
        var client = CreateOpenAIClient(settings, connectionName);
        var model = RequireModel(settings.EmbeddingModel, connectionName, "embedding");

        IEmbeddingGenerator<string, Embedding<float>> generator = client.GetEmbeddingClient(model).AsIEmbeddingGenerator();

        if (serviceKey is null)
        {
            builder.Services.AddSingleton(generator);
        }
        else
        {
            builder.Services.AddKeyedSingleton(serviceKey, generator);
        }
    }

    private static CloudflareAISettings ResolveSettings(
        IHostApplicationBuilder builder,
        string connectionName,
        Action<CloudflareAISettings>? configureSettings)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        var settings = new CloudflareAISettings();
        builder.Configuration.GetSection($"{DefaultConfigSectionRoot}:{connectionName}").Bind(settings);
        settings.ApplyConnectionString(builder.Configuration.GetConnectionString(connectionName));
        configureSettings?.Invoke(settings);
        return settings;
    }

    private static OpenAIClient CreateOpenAIClient(CloudflareAISettings settings, string connectionName)
    {
        if (string.IsNullOrWhiteSpace(settings.Endpoint))
        {
            throw new InvalidOperationException(
                $"No AI endpoint configured for connection '{connectionName}'. Ensure the AppHost references the " +
                $"Cloudflare AI resource (e.g. WithReference) or set '{DefaultConfigSectionRoot}:{connectionName}:Endpoint'.");
        }

        // The OpenAI SDK requires a non-empty credential; Ollama ignores it, so use a placeholder when absent.
        var key = string.IsNullOrEmpty(settings.Key) ? "ollama" : settings.Key;
        var options = new OpenAIClientOptions { Endpoint = new Uri(settings.Endpoint) };
        return new OpenAIClient(new ApiKeyCredential(key), options);
    }

    private static string RequireModel(string? model, string connectionName, string kind)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new InvalidOperationException(
                $"No {kind} model configured for AI connection '{connectionName}'. Set it on the AppHost " +
                $"(AddCloudflareAI options) or via settings.");
        }

        return model;
    }
}
