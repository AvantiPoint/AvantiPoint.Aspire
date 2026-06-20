using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api.Models;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.R2.Publishing;

/// <summary>
/// Publish target that provisions R2 buckets via the Cloudflare REST API during <c>aspire deploy</c>.
/// R2 has no build-time artifacts, so the publish phase is a no-op.
/// </summary>
internal sealed class R2PublishTarget(ILogger<R2PublishTarget> logger) : ICloudflarePublishTarget
{
    public bool CanHandle(IResource resource) => resource is R2BucketResource;

    public Task GenerateArtifactsAsync(CloudflarePublishContext context, IResource resource, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public async Task DeployAsync(CloudflareDeployContext context, IResource resource, CancellationToken cancellationToken)
    {
        var bucket = (R2BucketResource)resource;
        var apiClient = context.Services.GetRequiredService<ICloudflareApiClient>();

        logger.LogInformation("Provisioning R2 bucket '{Bucket}'...", bucket.BucketName);
        await apiClient.CreateR2BucketAsync(context.ApiToken, context.AccountId, new CreateR2BucketRequest
        {
            Name = bucket.BucketName,
            LocationHint = bucket.LocationHint,
        }, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Provisioned R2 bucket '{Bucket}'.", bucket.BucketName);
    }

    public async Task DestroyAsync(CloudflareDeployContext context, IResource resource, CancellationToken cancellationToken)
    {
        var bucket = (R2BucketResource)resource;
        if (!bucket.AllowDestroy)
        {
            logger.LogWarning(
                "Skipping deletion of R2 bucket '{Bucket}'. Call .AllowDeletion() on the bucket to permit destroy.",
                bucket.BucketName);
            return;
        }

        var apiClient = context.Services.GetRequiredService<ICloudflareApiClient>();
        await apiClient.DeleteR2BucketAsync(context.ApiToken, context.AccountId, bucket.BucketName, cancellationToken)
            .ConfigureAwait(false);
        logger.LogInformation("Deleted R2 bucket '{Bucket}'.", bucket.BucketName);
    }
}
