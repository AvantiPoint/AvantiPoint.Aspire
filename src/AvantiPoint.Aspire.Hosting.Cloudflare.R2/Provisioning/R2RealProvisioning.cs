using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.R2.Provisioning;

/// <summary>
/// Tracks R2 buckets that should be provisioned against a real Cloudflare account during
/// <c>aspire run</c> (i.e. <c>RunAsReal</c>), and provisions them once before the app starts.
/// </summary>
internal sealed class R2RealProvisioningAnnotation : IResourceAnnotation
{
    public List<R2BucketResource> Buckets { get; } = [];
}

internal static class R2RealProvisioning
{
    /// <summary>Registers <paramref name="bucket"/> for real provisioning and ensures the
    /// before-start provisioner is subscribed exactly once for the environment.</summary>
    public static void Register(
        IDistributedApplicationBuilder builder,
        CloudflareEnvironmentResource environment,
        R2BucketResource bucket)
    {
        if (!environment.TryGetLastAnnotation<R2RealProvisioningAnnotation>(out var annotation))
        {
            annotation = new R2RealProvisioningAnnotation();
            environment.Annotations.Add(annotation);

            builder.Eventing.Subscribe<BeforeStartEvent>((evt, ct) =>
                ProvisionAsync(environment, annotation, evt.Services, ct));
        }

        if (!annotation.Buckets.Contains(bucket))
        {
            annotation.Buckets.Add(bucket);
        }
    }

    private static async Task ProvisionAsync(
        CloudflareEnvironmentResource environment,
        R2RealProvisioningAnnotation annotation,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        if (annotation.Buckets.Count == 0)
        {
            return;
        }

        var logger = services.GetService<ILoggerFactory>()?.CreateLogger("Cloudflare.R2.Provisioning");
        var apiClient = services.GetRequiredService<ICloudflareApiClient>();
        var validator = services.GetRequiredService<CloudflareTokenValidator>();

        var token = await environment.ApiToken.GetValueAsync(cancellationToken).ConfigureAwait(false) ?? string.Empty;
        var accountId = await environment.AccountId.GetValueAsync(cancellationToken).ConfigureAwait(false) ?? string.Empty;

        await validator.ValidateAsync(token, environment.RequiredScopes, cancellationToken).ConfigureAwait(false);

        foreach (var bucket in annotation.Buckets)
        {
            logger?.LogInformation("Provisioning real R2 bucket '{Bucket}'...", bucket.BucketName);
            await apiClient.CreateR2BucketAsync(token, accountId, new CreateR2BucketRequest
            {
                Name = bucket.BucketName,
                LocationHint = bucket.LocationHint,
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}
