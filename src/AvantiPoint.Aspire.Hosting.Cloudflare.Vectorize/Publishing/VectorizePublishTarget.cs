using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api.Models;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Vectorize.Publishing;

/// <summary>
/// Publish target that provisions Vectorize indexes via the Cloudflare REST API during <c>aspire deploy</c>.
/// Vectorize has no build-time artifacts, so the publish phase is a no-op.
/// </summary>
internal sealed class VectorizePublishTarget(ILogger<VectorizePublishTarget> logger) : ICloudflarePublishTarget
{
    public bool CanHandle(IResource resource) => resource is VectorizeIndexResource;

    public Task GenerateArtifactsAsync(CloudflarePublishContext context, IResource resource, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public async Task DeployAsync(CloudflareDeployContext context, IResource resource, CancellationToken cancellationToken)
    {
        var index = (VectorizeIndexResource)resource;
        var apiClient = context.Services.GetRequiredService<ICloudflareApiClient>();

        logger.LogInformation("Provisioning Vectorize index '{Index}' ({Dimensions}d, {Metric})...",
            index.IndexName, index.Dimensions, index.Metric);
        await apiClient.CreateVectorizeIndexAsync(context.ApiToken, context.AccountId, new CreateVectorizeIndexRequest
        {
            Name = index.IndexName,
            Config = new VectorizeIndexConfig
            {
                Dimensions = index.Dimensions,
                Metric = index.Metric.ToApiValue(),
            },
        }, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Provisioned Vectorize index '{Index}'.", index.IndexName);
    }

    public async Task DestroyAsync(CloudflareDeployContext context, IResource resource, CancellationToken cancellationToken)
    {
        var index = (VectorizeIndexResource)resource;
        if (!index.AllowDestroy)
        {
            logger.LogWarning(
                "Skipping deletion of Vectorize index '{Index}'. Call .AllowDeletion() on the index to permit destroy.",
                index.IndexName);
            return;
        }

        var apiClient = context.Services.GetRequiredService<ICloudflareApiClient>();
        await apiClient.DeleteVectorizeIndexAsync(context.ApiToken, context.AccountId, index.IndexName, cancellationToken)
            .ConfigureAwait(false);
        logger.LogInformation("Deleted Vectorize index '{Index}'.", index.IndexName);
    }
}
