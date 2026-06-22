using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api.Models;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Queues.Publishing;

/// <summary>
/// Publish target that provisions Queues via the Cloudflare REST API during <c>aspire deploy</c>.
/// Queues have no build-time artifacts, so the publish phase is a no-op.
/// </summary>
internal sealed class QueuePublishTarget(ILogger<QueuePublishTarget> logger) : ICloudflarePublishTarget
{
    public bool CanHandle(IResource resource) => resource is QueueResource;

    public Task GenerateArtifactsAsync(CloudflarePublishContext context, IResource resource, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public async Task DeployAsync(CloudflareDeployContext context, IResource resource, CancellationToken cancellationToken)
    {
        var queue = (QueueResource)resource;
        var apiClient = context.Services.GetRequiredService<ICloudflareApiClient>();

        logger.LogInformation("Provisioning Queue '{Queue}'...", queue.QueueName);
        var result = await apiClient.CreateQueueAsync(context.ApiToken, context.AccountId, new CreateQueueRequest
        {
            QueueName = queue.QueueName,
        }, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Provisioned Queue '{Queue}' ({Id}).", queue.QueueName, result.QueueId);
    }

    public async Task DestroyAsync(CloudflareDeployContext context, IResource resource, CancellationToken cancellationToken)
    {
        var queue = (QueueResource)resource;
        if (!queue.AllowDestroy)
        {
            logger.LogWarning(
                "Skipping deletion of Queue '{Queue}'. Call .AllowDeletion() to permit destroy.",
                queue.QueueName);
            return;
        }

        var apiClient = context.Services.GetRequiredService<ICloudflareApiClient>();
        var existing = await apiClient.GetQueueByNameAsync(context.ApiToken, context.AccountId, queue.QueueName, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
        {
            return;
        }

        await apiClient.DeleteQueueAsync(context.ApiToken, context.AccountId, existing.QueueId, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Deleted Queue '{Queue}'.", queue.QueueName);
    }
}
