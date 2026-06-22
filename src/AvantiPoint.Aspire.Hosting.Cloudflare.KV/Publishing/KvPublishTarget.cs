using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api.Models;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.KV.Publishing;

/// <summary>
/// Publish target that provisions Workers KV namespaces via the Cloudflare REST API during
/// <c>aspire deploy</c>. KV has no build-time artifacts, so the publish phase is a no-op.
/// </summary>
internal sealed class KvPublishTarget(ILogger<KvPublishTarget> logger) : ICloudflarePublishTarget
{
    public bool CanHandle(IResource resource) => resource is KvNamespaceResource;

    public Task GenerateArtifactsAsync(CloudflarePublishContext context, IResource resource, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public async Task DeployAsync(CloudflareDeployContext context, IResource resource, CancellationToken cancellationToken)
    {
        var ns = (KvNamespaceResource)resource;
        var apiClient = context.Services.GetRequiredService<ICloudflareApiClient>();

        logger.LogInformation("Provisioning KV namespace '{Namespace}'...", ns.Title);
        var result = await apiClient.CreateKvNamespaceAsync(context.ApiToken, context.AccountId, new CreateKvNamespaceRequest
        {
            Title = ns.Title,
        }, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Provisioned KV namespace '{Namespace}' ({Id}).", ns.Title, result.Id);
    }

    public async Task DestroyAsync(CloudflareDeployContext context, IResource resource, CancellationToken cancellationToken)
    {
        var ns = (KvNamespaceResource)resource;
        if (!ns.AllowDestroy)
        {
            logger.LogWarning(
                "Skipping deletion of KV namespace '{Namespace}'. Call .AllowDeletion() to permit destroy.",
                ns.Title);
            return;
        }

        var apiClient = context.Services.GetRequiredService<ICloudflareApiClient>();
        var existing = await apiClient.GetKvNamespaceByTitleAsync(context.ApiToken, context.AccountId, ns.Title, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
        {
            return;
        }

        await apiClient.DeleteKvNamespaceAsync(context.ApiToken, context.AccountId, existing.Id, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Deleted KV namespace '{Namespace}'.", ns.Title);
    }
}
