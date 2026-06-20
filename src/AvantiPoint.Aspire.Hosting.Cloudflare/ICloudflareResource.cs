using Aspire.Hosting.ApplicationModel;

namespace AvantiPoint.Aspire.Hosting.Cloudflare;

/// <summary>
/// A resource that is provisioned/deployed into a <see cref="CloudflareEnvironmentResource"/>.
/// The pipeline steps use this to find the resources that belong to a given environment.
/// </summary>
public interface ICloudflareResource : IResource
{
    /// <summary>The Cloudflare environment this resource is deployed into.</summary>
    CloudflareEnvironmentResource Environment { get; }
}
