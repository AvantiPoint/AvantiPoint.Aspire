using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Vectorize;

/// <summary>
/// A Cloudflare Vectorize index modeled as an Aspire resource. Exposes a connection string that targets
/// the real Vectorize v2 REST API by default (in run and deploy), or an in-process in-memory vector store
/// during <c>aspire run</c> when <see cref="VectorizeHostingExtensions.RunAsEmulator"/> is used.
/// </summary>
[AspireExport]
public sealed class VectorizeIndexResource : Resource, IResourceWithConnectionString, ICloudflareResource
{
    internal VectorizeIndexResource(
        string name,
        CloudflareEnvironmentResource environment,
        string indexName,
        int dimensions,
        VectorizeMetric metric)
        : base(name)
    {
        Environment = environment;
        IndexName = indexName;
        Dimensions = dimensions;
        Metric = metric;
    }

    /// <summary>The Cloudflare environment that owns this index.</summary>
    public CloudflareEnvironmentResource Environment { get; }

    /// <summary>The Vectorize index name (may differ from the Aspire resource name).</summary>
    public string IndexName { get; }

    /// <summary>The vector dimensionality (must match the embedding model that produces the vectors).</summary>
    public int Dimensions { get; }

    /// <summary>The distance metric.</summary>
    public VectorizeMetric Metric { get; }

    /// <summary>True when this index is served by the in-memory emulator (opt-in via <c>RunAsEmulator()</c>).</summary>
    public bool UseEmulator { get; internal set; }

    /// <summary>When true, <c>aspire deploy --destroy</c> will delete the real index. Off by default (data-loss guard).</summary>
    public bool AllowDestroy { get; internal set; }

    // Optional token override for runtime access; defaults to the environment API token.
    internal ParameterResource? AccessToken { get; set; }

    /// <inheritdoc />
    public ReferenceExpression ConnectionStringExpression => BuildConnectionString();

    private ReferenceExpression BuildConnectionString()
    {
        var metric = Metric.ToApiValue();

        if (UseEmulator)
        {
            return ReferenceExpression.Create(
                $"Provider=InMemory;Index={IndexName};Dimensions={Dimensions.ToString()};Metric={metric}");
        }

        var accountId = Environment.AccountId;
        var token = AccessToken ?? Environment.ApiToken;

        return ReferenceExpression.Create(
            $"Provider=Vectorize;AccountId={accountId};Index={IndexName};Dimensions={Dimensions.ToString()};Metric={metric};Token={token}");
    }
}
