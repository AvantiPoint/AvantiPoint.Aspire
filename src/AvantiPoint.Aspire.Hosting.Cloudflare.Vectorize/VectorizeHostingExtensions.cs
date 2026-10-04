using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;
using AvantiPoint.Aspire.Hosting.Cloudflare.Vectorize.Provisioning;
using AvantiPoint.Aspire.Hosting.Cloudflare.Vectorize.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Vectorize;

/// <summary>Extension methods for adding Cloudflare Vectorize indexes to an Aspire application.</summary>
public static class VectorizeHostingExtensions
{
    /// <summary>
    /// Adds a Vectorize index, using the single Cloudflare environment added to the application. Add one
    /// with <see cref="CloudflareEnvironmentExtensions.AddCloudflareEnvironment"/> first.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The Aspire resource name (also the connection name consumers reference).</param>
    /// <param name="dimensions">The vector dimensionality (match your embedding model).</param>
    /// <param name="metric">The distance metric. Defaults to cosine.</param>
    /// <param name="indexName">The Vectorize index name; defaults to <paramref name="name"/>.</param>
    [AspireExport("addVectorizeIndex")]
    public static IResourceBuilder<VectorizeIndexResource> AddVectorizeIndex(
        this IDistributedApplicationBuilder builder,
        string name,
        int dimensions,
        VectorizeMetric metric = VectorizeMetric.Cosine,
        string? indexName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.GetCloudflareEnvironment().AddVectorizeIndex(name, dimensions, metric, indexName);
    }

    /// <summary>
    /// Adds a Vectorize index to a specific Cloudflare environment. By default it targets <b>real
    /// Vectorize</b> (provisioned at <c>aspire deploy</c>). Call <see cref="RunAsEmulator"/> to back it
    /// with an in-memory vector store during <c>aspire run</c>.
    /// </summary>
    /// <param name="environment">The Cloudflare environment builder.</param>
    /// <param name="name">The Aspire resource name (also the connection name consumers reference).</param>
    /// <param name="dimensions">The vector dimensionality (match your embedding model).</param>
    /// <param name="metric">The distance metric. Defaults to cosine.</param>
    /// <param name="indexName">The Vectorize index name; defaults to <paramref name="name"/>.</param>
    [AspireExport("addVectorizeIndexInEnvironment")]
    public static IResourceBuilder<VectorizeIndexResource> AddVectorizeIndex(
        this IResourceBuilder<CloudflareEnvironmentResource> environment,
        string name,
        int dimensions,
        VectorizeMetric metric = VectorizeMetric.Cosine,
        string? indexName = null)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dimensions);

        var builder = environment.ApplicationBuilder;
        environment.Resource.RequireScopes(CloudflareScopes.VectorizeEdit);

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ICloudflarePublishTarget, VectorizePublishTarget>());

        var resource = new VectorizeIndexResource(name, environment.Resource, indexName ?? name, dimensions, metric);
        var index = builder.AddResource(resource);

        // In run mode the real index is provisioned on start (skipped for indexes using the emulator).
        if (builder.ExecutionContext.IsRunMode)
        {
            VectorizeRealProvisioning.Register(builder, environment.Resource, resource);
        }

        return index;
    }

    /// <summary>
    /// Backs this index with an in-process in-memory vector store during <c>aspire run</c> (no Cloudflare
    /// credentials required). Ignored during <c>aspire publish</c>/<c>deploy</c>, which always use real
    /// Vectorize. Mirrors the <c>RunAsEmulator()</c> convention of Aspire's Azure integrations.
    /// </summary>
    [AspireExport("runAsEmulator")]
    public static IResourceBuilder<VectorizeIndexResource> RunAsEmulator(this IResourceBuilder<VectorizeIndexResource> index)
    {
        ArgumentNullException.ThrowIfNull(index);

        if (index.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            index.Resource.UseEmulator = true;
        }

        return index;
    }

    /// <summary>
    /// Uses a specific API token (a parameter) for runtime Vectorize access, instead of the environment's
    /// Cloudflare API token.
    /// </summary>
    [AspireExport("withAccessToken")]
    public static IResourceBuilder<VectorizeIndexResource> WithAccessToken(
        this IResourceBuilder<VectorizeIndexResource> index,
        IResourceBuilder<ParameterResource> token)
    {
        index.Resource.AccessToken = token.Resource;
        return index;
    }

    /// <summary>Permits <c>aspire destroy</c> to delete this real index. Off by default (data-loss guard).</summary>
    [AspireExport("allowDeletion")]
    public static IResourceBuilder<VectorizeIndexResource> AllowDeletion(this IResourceBuilder<VectorizeIndexResource> index)
    {
        index.Resource.AllowDestroy = true;
        return index;
    }
}
