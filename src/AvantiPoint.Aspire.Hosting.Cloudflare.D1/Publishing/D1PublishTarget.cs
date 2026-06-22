using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api.Models;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.D1.Publishing;

/// <summary>
/// Publish target that provisions D1 databases via the Cloudflare REST API during <c>aspire deploy</c>.
/// D1 has no build-time artifacts, so the publish phase is a no-op.
/// </summary>
internal sealed class D1PublishTarget(ILogger<D1PublishTarget> logger) : ICloudflarePublishTarget
{
    public bool CanHandle(IResource resource) => resource is D1DatabaseResource;

    public Task GenerateArtifactsAsync(CloudflarePublishContext context, IResource resource, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public async Task DeployAsync(CloudflareDeployContext context, IResource resource, CancellationToken cancellationToken)
    {
        var database = (D1DatabaseResource)resource;
        var apiClient = context.Services.GetRequiredService<ICloudflareApiClient>();

        logger.LogInformation("Provisioning D1 database '{Database}'...", database.DatabaseName);
        var result = await apiClient.CreateD1DatabaseAsync(context.ApiToken, context.AccountId, new CreateD1DatabaseRequest
        {
            Name = database.DatabaseName,
            PrimaryLocationHint = database.PrimaryLocationHint,
        }, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Provisioned D1 database '{Database}' ({Uuid}).", database.DatabaseName, result.Uuid);
    }

    public async Task DestroyAsync(CloudflareDeployContext context, IResource resource, CancellationToken cancellationToken)
    {
        var database = (D1DatabaseResource)resource;
        if (!database.AllowDestroy)
        {
            logger.LogWarning(
                "Skipping deletion of D1 database '{Database}'. Call .AllowDeletion() on the database to permit destroy.",
                database.DatabaseName);
            return;
        }

        var apiClient = context.Services.GetRequiredService<ICloudflareApiClient>();
        var existing = await apiClient.GetD1DatabaseByNameAsync(context.ApiToken, context.AccountId, database.DatabaseName, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
        {
            return;
        }

        await apiClient.DeleteD1DatabaseAsync(context.ApiToken, context.AccountId, existing.Uuid, cancellationToken)
            .ConfigureAwait(false);
        logger.LogInformation("Deleted D1 database '{Database}'.", database.DatabaseName);
    }
}
