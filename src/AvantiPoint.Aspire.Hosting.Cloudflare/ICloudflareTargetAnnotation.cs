using Aspire.Hosting.ApplicationModel;

namespace AvantiPoint.Aspire.Hosting.Cloudflare;

/// <summary>
/// Annotation that associates an existing Aspire resource (e.g. a JavaScript app for Pages, or a
/// project for a Container) with a <see cref="CloudflareEnvironmentResource"/>. The pipeline uses
/// this — in addition to <see cref="ICloudflareResource"/> — to discover the resources that belong
/// to an environment, so deployment can attach to resources Aspire already models.
/// </summary>
public interface ICloudflareTargetAnnotation : IResourceAnnotation
{
    /// <summary>The Cloudflare environment the annotated resource deploys into.</summary>
    CloudflareEnvironmentResource Environment { get; }
}
