using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;

/// <summary>
/// Maps an Aspire resource to a Cloudflare deployment action — the Cloudflare analog of the AWS
/// integration's publish targets. Satellite packages (R2, Pages, ...) register implementations into
/// DI; the pipeline's publish/deploy/destroy steps dispatch each resource to the first target whose
/// <see cref="CanHandle"/> returns <c>true</c>.
/// </summary>
public interface ICloudflarePublishTarget
{
    /// <summary>Whether this target handles the given resource.</summary>
    bool CanHandle(IResource resource);

    /// <summary>Generates any provider-native artifacts (wrangler config, Dockerfiles, staged output) for the resource.</summary>
    Task GenerateArtifactsAsync(CloudflarePublishContext context, IResource resource, CancellationToken cancellationToken);

    /// <summary>Applies the resource to Cloudflare (REST provisioning and/or wrangler deploy).</summary>
    Task DeployAsync(CloudflareDeployContext context, IResource resource, CancellationToken cancellationToken);

    /// <summary>Tears the resource down from Cloudflare.</summary>
    Task DestroyAsync(CloudflareDeployContext context, IResource resource, CancellationToken cancellationToken);
}

/// <summary>Context for the publish (artifact-generation) phase.</summary>
public sealed class CloudflarePublishContext
{
    public required PipelineStepContext Step { get; init; }
    public required CloudflareEnvironmentResource Environment { get; init; }

    public IServiceProvider Services => Step.Services;
}

/// <summary>Context for the deploy/destroy phases, with the resolved (validated) credentials.</summary>
public sealed class CloudflareDeployContext
{
    public required PipelineStepContext Step { get; init; }
    public required CloudflareEnvironmentResource Environment { get; init; }

    /// <summary>The validated Cloudflare API token.</summary>
    public required string ApiToken { get; init; }

    /// <summary>The Cloudflare account id.</summary>
    public required string AccountId { get; init; }

    public IServiceProvider Services => Step.Services;
}
