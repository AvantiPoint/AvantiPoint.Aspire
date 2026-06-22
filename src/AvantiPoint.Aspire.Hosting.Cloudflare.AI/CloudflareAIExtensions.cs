using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.AI.Emulator;
using AvantiPoint.Aspire.Hosting.Cloudflare.AI.Publishing;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.AI;

/// <summary>Extension methods for adding Cloudflare AI to an Aspire application.</summary>
public static class CloudflareAIExtensions
{
    /// <summary>
    /// Adds a Cloudflare AI resource, using the single Cloudflare environment added to the application.
    /// Add one with <see cref="CloudflareEnvironmentExtensions.AddCloudflareEnvironment"/> first.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The Aspire resource name (also the connection name consumers reference).</param>
    /// <param name="configure">Optional model/gateway configuration.</param>
    public static IResourceBuilder<CloudflareAIResource> AddCloudflareAI(
        this IDistributedApplicationBuilder builder,
        string name,
        Action<CloudflareAIOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.GetCloudflareEnvironment().AddCloudflareAI(name, configure);
    }

    /// <summary>
    /// Adds a Cloudflare AI resource to a specific Cloudflare environment. By default it targets <b>real
    /// Cloudflare AI</b> (Workers AI, or an AI Gateway when <see cref="CloudflareAIOptions.GatewayId"/> is
    /// set). Call <see cref="RunAsEmulator"/> to serve it from a local Ollama server during <c>aspire run</c>.
    /// </summary>
    /// <param name="environment">The Cloudflare environment builder.</param>
    /// <param name="name">The Aspire resource name (also the connection name consumers reference).</param>
    /// <param name="configure">Optional model/gateway configuration.</param>
    public static IResourceBuilder<CloudflareAIResource> AddCloudflareAI(
        this IResourceBuilder<CloudflareEnvironmentResource> environment,
        string name,
        Action<CloudflareAIOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(environment);

        var builder = environment.ApplicationBuilder;
        var options = new CloudflareAIOptions();
        configure?.Invoke(options);

        environment.Resource.RequireScopes(CloudflareScopes.WorkersAIRead);
        if (!string.IsNullOrEmpty(options.GatewayId))
        {
            environment.Resource.RequireScopes(CloudflareScopes.AIGatewayEdit);
        }

        // Register the AI Gateway publish target so the deploy pipeline provisions a gateway when requested.
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ICloudflarePublishTarget, AIGatewayPublishTarget>());

        var resource = new CloudflareAIResource(name, environment.Resource, options);
        return builder.AddResource(resource);
    }

    /// <summary>
    /// Serves this AI resource from a local <b>Ollama container</b> during <c>aspire run</c> (Docker
    /// required, no Cloudflare credentials). The configured local models are pulled automatically and
    /// persisted in a volume. Ignored during <c>aspire publish</c>/<c>deploy</c>, which always use real
    /// Cloudflare AI. Mirrors the <c>RunAsEmulator()</c> convention of Aspire's Azure integrations.
    /// See <see cref="RunOnHost"/> to use a host-installed Ollama instead of a container.
    /// </summary>
    /// <param name="ai">The AI resource builder.</param>
    /// <param name="configureOllama">Optional configuration of the shared Ollama container (GPU, ports, ...).</param>
    public static IResourceBuilder<CloudflareAIResource> RunAsEmulator(
        this IResourceBuilder<CloudflareAIResource> ai,
        Action<IResourceBuilder<OllamaResource>>? configureOllama = null)
    {
        ArgumentNullException.ThrowIfNull(ai);

        if (!ai.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            return ai; // publish/deploy always uses real Cloudflare AI
        }

        ai.Resource.UseEmulator = true;
        ai.Resource.EmulatorEndpoint = OllamaEmulator.GetOrAddContainer(ai, configureOllama);
        return ai;
    }

    /// <summary>
    /// Serves this AI resource from a <b>host-installed Ollama</b> during <c>aspire run</c> — the same
    /// credential-free local dev loop as <see cref="RunAsEmulator"/>, but attaching to Ollama running
    /// natively on the host (no container/Docker), which is useful for native GPU access. The configured
    /// local models are pulled automatically. Ignored during <c>aspire publish</c>/<c>deploy</c>.
    /// </summary>
    /// <param name="ai">The AI resource builder.</param>
    /// <param name="configureOllama">Optional configuration of the shared host Ollama (ports, ...).</param>
    public static IResourceBuilder<CloudflareAIResource> RunOnHost(
        this IResourceBuilder<CloudflareAIResource> ai,
        Action<IResourceBuilder<OllamaExecutableResource>>? configureOllama = null)
    {
        ArgumentNullException.ThrowIfNull(ai);

        if (!ai.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            return ai; // publish/deploy always uses real Cloudflare AI
        }

        ai.Resource.UseEmulator = true;
        ai.Resource.EmulatorEndpoint = OllamaEmulator.GetOrAddHost(ai, configureOllama);
        return ai;
    }

    /// <summary>
    /// Uses a specific API token (a parameter) for runtime AI access, instead of the environment's
    /// Cloudflare API token. Useful to scope runtime access to just AI.
    /// </summary>
    public static IResourceBuilder<CloudflareAIResource> WithAccessToken(
        this IResourceBuilder<CloudflareAIResource> ai,
        IResourceBuilder<ParameterResource> token)
    {
        ai.Resource.AccessToken = token.Resource;
        return ai;
    }
}
