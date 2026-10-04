using Aspire.Hosting;
namespace AvantiPoint.Aspire.Hosting.Cloudflare;

/// <summary>Options for a Cloudflare Worker resource.</summary>
[AspireExport(ExposeProperties = true)]
public sealed class CloudflareWorkerOptions
{
    /// <summary>The port <c>wrangler dev</c> listens on during <c>aspire run</c>. Default 8787.</summary>
    public int Port { get; set; } = 8787;
}
