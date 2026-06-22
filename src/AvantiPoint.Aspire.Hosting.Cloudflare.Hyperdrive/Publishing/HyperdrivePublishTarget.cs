using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api.Models;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Hyperdrive.Publishing;

/// <summary>
/// Publish target that provisions a Hyperdrive configuration via the Cloudflare REST API during
/// <c>aspire deploy</c>. The production connection string is resolved and parsed into the Hyperdrive origin
/// fields. Hyperdrive has no build-time artifacts, so the publish phase is a no-op.
/// </summary>
internal sealed class HyperdrivePublishTarget(ILogger<HyperdrivePublishTarget> logger) : ICloudflarePublishTarget
{
    public bool CanHandle(IResource resource) => resource is HyperdriveResource;

    public Task GenerateArtifactsAsync(CloudflarePublishContext context, IResource resource, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public async Task DeployAsync(CloudflareDeployContext context, IResource resource, CancellationToken cancellationToken)
    {
        var hyperdrive = (HyperdriveResource)resource;
        var apiClient = context.Services.GetRequiredService<ICloudflareApiClient>();

        var connectionString = await hyperdrive.ProductionConnectionString.GetValueAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Hyperdrive '{hyperdrive.Name}' has no production connection string. Provide it via the deployment " +
                $"parameter (or connection-string resource) passed to PublishAsHyperdrive.");
        }

        var origin = PostgresConnectionStringParser.Parse(connectionString);

        logger.LogInformation("Provisioning Hyperdrive config '{Name}' for {Host}:{Port}/{Database}...",
            hyperdrive.Name, origin.Host, origin.Port, origin.Database);
        var config = await apiClient.CreateHyperdriveConfigAsync(context.ApiToken, context.AccountId, new CreateHyperdriveConfigRequest
        {
            Name = hyperdrive.Name,
            Origin = origin,
            Caching = new HyperdriveCaching { Disabled = hyperdrive.CachingDisabled },
        }, cancellationToken).ConfigureAwait(false);

        hyperdrive.ConfigId = config.Id;
        logger.LogInformation("Provisioned Hyperdrive config '{Name}' ({Id}).", hyperdrive.Name, config.Id);

        foreach (var (workerName, bindingName) in hyperdrive.Bindings)
        {
            logger.LogInformation(
                "Add this Hyperdrive binding to the '{Worker}' wrangler.jsonc: \"hyperdrive\": [{{ \"binding\": \"{Binding}\", \"id\": \"{Id}\" }}]",
                workerName, bindingName, config.Id);
        }
    }

    public async Task DestroyAsync(CloudflareDeployContext context, IResource resource, CancellationToken cancellationToken)
    {
        var hyperdrive = (HyperdriveResource)resource;
        if (!hyperdrive.AllowDestroy)
        {
            logger.LogWarning(
                "Skipping deletion of Hyperdrive config '{Name}'. Call .AllowDeletion() to permit destroy.",
                hyperdrive.Name);
            return;
        }

        var apiClient = context.Services.GetRequiredService<ICloudflareApiClient>();
        var existing = await apiClient.GetHyperdriveConfigByNameAsync(context.ApiToken, context.AccountId, hyperdrive.Name, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
        {
            return;
        }

        await apiClient.DeleteHyperdriveConfigAsync(context.ApiToken, context.AccountId, existing.Id, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Deleted Hyperdrive config '{Name}'.", hyperdrive.Name);
    }
}
