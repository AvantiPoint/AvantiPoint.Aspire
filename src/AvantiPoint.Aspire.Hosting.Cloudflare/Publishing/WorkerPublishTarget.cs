using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare.Cli;
using Microsoft.Extensions.Logging;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;

/// <summary>
/// Publish target for hand-authored Cloudflare Workers. The Worker owns its wrangler config and source,
/// so publish is a no-op; deploy/destroy shell out to <c>wrangler</c> in the Worker directory.
/// </summary>
internal sealed class WorkerPublishTarget(ILogger<WorkerPublishTarget> logger, IWranglerCli wrangler) : ICloudflarePublishTarget
{
    public bool CanHandle(IResource resource) => resource is CloudflareWorkerResource;

    public Task GenerateArtifactsAsync(CloudflarePublishContext context, IResource resource, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public async Task DeployAsync(CloudflareDeployContext context, IResource resource, CancellationToken cancellationToken)
    {
        var worker = (CloudflareWorkerResource)resource;
        logger.LogInformation("Deploying Worker '{Worker}' to Cloudflare...", worker.Name);
        await wrangler.RunAsync(
            ["deploy"],
            workingDirectory: worker.WorkerDirectory,
            apiToken: context.ApiToken,
            accountId: context.AccountId,
            logger: logger,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Deployed Worker '{Worker}'.", worker.Name);
    }

    public async Task DestroyAsync(CloudflareDeployContext context, IResource resource, CancellationToken cancellationToken)
    {
        var worker = (CloudflareWorkerResource)resource;
        await wrangler.RunAsync(
            ["delete", "--force"],
            workingDirectory: worker.WorkerDirectory,
            apiToken: context.ApiToken,
            accountId: context.AccountId,
            logger: logger,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
