using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Hyperdrive;

/// <summary>
/// A Cloudflare Hyperdrive configuration modeled as an Aspire resource. Hyperdrive accelerates and pools
/// connections to an existing (often third-party) database; it is consumed by a Worker via a binding, not
/// over HTTP, so there is no .NET client. This resource is provisioned at <c>aspire deploy</c> from a
/// <b>production</b> connection string — never the local dev container.
/// </summary>
public sealed class HyperdriveResource : Resource, ICloudflareResource
{
    internal HyperdriveResource(string name, CloudflareEnvironmentResource environment, ReferenceExpression productionConnectionString)
        : base(name)
    {
        Environment = environment;
        ProductionConnectionString = productionConnectionString;
    }

    /// <summary>The Cloudflare environment that owns this configuration.</summary>
    public CloudflareEnvironmentResource Environment { get; }

    /// <summary>The production database connection string the Hyperdrive config points at (resolved at deploy).</summary>
    public ReferenceExpression ProductionConnectionString { get; }

    /// <summary>Disables Hyperdrive query caching when true.</summary>
    public bool CachingDisabled { get; internal set; }

    /// <summary>When true, <c>aspire deploy --destroy</c> will delete the Hyperdrive config. Off by default.</summary>
    public bool AllowDestroy { get; internal set; }

    /// <summary>The provisioned Hyperdrive config id (available after deploy).</summary>
    public string? ConfigId { get; internal set; }

    /// <summary>Workers that bind this Hyperdrive config (worker name + binding variable name).</summary>
    internal List<(string WorkerName, string BindingName)> Bindings { get; } = [];
}
