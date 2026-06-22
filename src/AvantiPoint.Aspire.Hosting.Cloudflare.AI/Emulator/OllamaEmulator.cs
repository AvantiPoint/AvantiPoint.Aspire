using System.Text;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.AI.Emulator;

/// <summary>
/// Adds and shares a local Ollama server (one per Cloudflare environment) used as the AI emulator under
/// <c>RunAsEmulator()</c> (a container) or <c>RunOnHost()</c> (a host-installed Ollama). Each AI resource
/// registers its configured local chat/embedding models, and the connection string is pointed at Ollama's
/// OpenAI-compatible <c>/v1</c> endpoint — mirroring how the MinIO emulator backs R2.
/// </summary>
internal sealed class OllamaEmulatorAnnotation(
    EndpointReference endpoint,
    Action<string> addModel,
    Action<IResourceBuilder<CloudflareAIResource>> wireWaitFor) : IResourceAnnotation
{
    public EndpointReference Endpoint { get; } = endpoint;

    /// <summary>Adds a model by its Ollama id (the caller deduplicates via <see cref="Models"/>).</summary>
    public Action<string> AddModel { get; } = addModel;

    /// <summary>Wires an AI resource to wait for the (strongly-typed) Ollama resource.</summary>
    public Action<IResourceBuilder<CloudflareAIResource>> WireWaitFor { get; } = wireWaitFor;

    public HashSet<string> Models { get; } = new(StringComparer.OrdinalIgnoreCase);
}

internal static class OllamaEmulator
{
    /// <summary>Backs the AI resource with a containerized Ollama (Docker), persisting models in a volume.</summary>
    public static EndpointReference GetOrAddContainer(
        IResourceBuilder<CloudflareAIResource> ai,
        Action<IResourceBuilder<OllamaResource>>? configureOllama)
        => GetOrAdd(ai, () =>
        {
            var ollama = ai.ApplicationBuilder.AddOllama($"{ai.Resource.Environment.Name}-ollama").WithDataVolume();
            configureOllama?.Invoke(ollama);
            return (
                ollama.Resource.PrimaryEndpoint,
                id => ollama.AddModel(SanitizeResourceName(id), id),
                builder => builder.WaitFor(ollama));
        });

    /// <summary>Backs the AI resource with a host-installed Ollama (no container/Docker).</summary>
    public static EndpointReference GetOrAddHost(
        IResourceBuilder<CloudflareAIResource> ai,
        Action<IResourceBuilder<OllamaExecutableResource>>? configureOllama)
        => GetOrAdd(ai, () =>
        {
            var ollama = ai.ApplicationBuilder.AddOllamaLocal($"{ai.Resource.Environment.Name}-ollama");
            configureOllama?.Invoke(ollama);
            return (
                ollama.Resource.PrimaryEndpoint,
                id => ollama.AddModel(SanitizeResourceName(id), id),
                builder => builder.WaitFor(ollama));
        });

    private static EndpointReference GetOrAdd(
        IResourceBuilder<CloudflareAIResource> ai,
        Func<(EndpointReference Endpoint, Action<string> AddModel, Action<IResourceBuilder<CloudflareAIResource>> WireWaitFor)> create)
    {
        var environment = ai.Resource.Environment;

        if (!environment.TryGetLastAnnotation<OllamaEmulatorAnnotation>(out var annotation))
        {
            var (endpoint, addModel, wireWaitFor) = create();
            annotation = new OllamaEmulatorAnnotation(endpoint, addModel, wireWaitFor);
            environment.Annotations.Add(annotation);
        }

        var options = ai.Resource.Options;
        AddModel(annotation, options.LocalChatModel);
        AddModel(annotation, options.LocalEmbeddingModel);

        // The AI resource (and anything that waits on it) waits for Ollama to be running.
        annotation.WireWaitFor(ai);

        return annotation.Endpoint;
    }

    private static void AddModel(OllamaEmulatorAnnotation annotation, string model)
    {
        if (annotation.Models.Add(model))
        {
            annotation.AddModel(model);
        }
    }

    // Ollama model ids (e.g. "llama3.2") may contain characters invalid in an Aspire resource name.
    private static string SanitizeResourceName(string model)
    {
        var builder = new StringBuilder(model.Length);
        foreach (var c in model.ToLowerInvariant())
        {
            builder.Append(char.IsLetterOrDigit(c) ? c : '-');
        }

        return builder.ToString().Trim('-');
    }
}
