using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace AvantiPoint.Aspire.Hosting.Cloudflare;

/// <summary>
/// A hand-authored Cloudflare Worker (its own <c>wrangler.toml</c>/<c>wrangler.jsonc</c> + source). It runs
/// locally via <c>wrangler dev</c> (Miniflare) during <c>aspire run</c> and is deployed with
/// <c>wrangler deploy</c> during <c>aspire deploy</c>.
/// </summary>
[AspireExport]
public sealed class CloudflareWorkerResource : ExecutableResource, ICloudflareResource
{
    internal CloudflareWorkerResource(string name, string command, string workingDirectory, CloudflareEnvironmentResource environment)
        : base(name, command, workingDirectory)
    {
        Environment = environment;
        WorkerDirectory = workingDirectory;
    }

    /// <inheritdoc />
    public CloudflareEnvironmentResource Environment { get; }

    /// <summary>The Worker project directory (contains its wrangler config and source).</summary>
    public string WorkerDirectory { get; }
}
