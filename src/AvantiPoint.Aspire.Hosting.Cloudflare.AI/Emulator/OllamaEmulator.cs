using System.Text;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.AI.Emulator;

/// <summary>
/// Adds and shares a local Ollama server (one per Cloudflare environment) used as the AI emulator under
/// <c>RunAsEmulator()</c>. Each AI resource registers its configured local chat/embedding models, and the
/// connection string is pointed at Ollama's OpenAI-compatible <c>/v1</c> endpoint — mirroring how the
/// MinIO emulator backs R2. The model files are persisted in a data volume so the (large) first pull is
/// only paid once.
/// </summary>
internal sealed class OllamaEmulatorAnnotation(IResourceBuilder<OllamaResource> ollama) : IResourceAnnotation
{
    public IResourceBuilder<OllamaResource> Ollama { get; } = ollama;

    public HashSet<string> Models { get; } = new(StringComparer.OrdinalIgnoreCase);
}

internal static class OllamaEmulator
{
    public static EndpointReference GetOrAdd(
        IResourceBuilder<CloudflareAIResource> ai,
        Action<IResourceBuilder<OllamaResource>>? configureOllama)
    {
        var builder = ai.ApplicationBuilder;
        var environment = ai.Resource.Environment;

        if (!environment.TryGetLastAnnotation<OllamaEmulatorAnnotation>(out var annotation))
        {
            var ollama = builder.AddOllama($"{environment.Name}-ollama").WithDataVolume();
            configureOllama?.Invoke(ollama);
            annotation = new OllamaEmulatorAnnotation(ollama);
            environment.Annotations.Add(annotation);
        }

        var options = ai.Resource.Options;
        AddModel(annotation, options.LocalChatModel);
        AddModel(annotation, options.LocalEmbeddingModel);

        // The AI resource (and anything that waits on it) waits for Ollama to be running.
        ai.WaitFor(annotation.Ollama);

        return annotation.Ollama.Resource.PrimaryEndpoint;
    }

    private static void AddModel(OllamaEmulatorAnnotation annotation, string model)
    {
        if (annotation.Models.Add(model))
        {
            annotation.Ollama.AddModel(SanitizeResourceName(model), model);
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
