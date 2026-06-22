using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Vectorize.Provisioning;

/// <summary>
/// Tracks Vectorize indexes that target real Cloudflare during <c>aspire run</c> (the default), and
/// provisions them once before the app starts. Indexes switched to the in-memory emulator with
/// <c>RunAsEmulator()</c> are skipped.
/// </summary>
internal sealed class VectorizeRealProvisioningAnnotation : IResourceAnnotation
{
    public List<VectorizeIndexResource> Indexes { get; } = [];
}

internal static class VectorizeRealProvisioning
{
    public static void Register(
        IDistributedApplicationBuilder builder,
        CloudflareEnvironmentResource environment,
        VectorizeIndexResource index)
    {
        if (!environment.TryGetLastAnnotation<VectorizeRealProvisioningAnnotation>(out var annotation))
        {
            annotation = new VectorizeRealProvisioningAnnotation();
            environment.Annotations.Add(annotation);

            builder.Eventing.Subscribe<BeforeStartEvent>((evt, ct) =>
                ProvisionAsync(environment, annotation, evt.Services, ct));
        }

        if (!annotation.Indexes.Contains(index))
        {
            annotation.Indexes.Add(index);
        }
    }

    private static async Task ProvisionAsync(
        CloudflareEnvironmentResource environment,
        VectorizeRealProvisioningAnnotation annotation,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var realIndexes = annotation.Indexes.Where(i => !i.UseEmulator).ToList();
        if (realIndexes.Count == 0)
        {
            return;
        }

        var logger = services.GetService<ILoggerFactory>()?.CreateLogger("Cloudflare.Vectorize.Provisioning");
        var apiClient = services.GetRequiredService<ICloudflareApiClient>();
        var validator = services.GetRequiredService<CloudflareTokenValidator>();

        var token = await environment.ApiToken.GetValueAsync(cancellationToken).ConfigureAwait(false) ?? string.Empty;
        var accountId = await environment.AccountId.GetValueAsync(cancellationToken).ConfigureAwait(false) ?? string.Empty;

        await validator.ValidateAsync(token, environment.RequiredScopes, cancellationToken).ConfigureAwait(false);

        foreach (var index in realIndexes)
        {
            logger?.LogInformation("Provisioning real Vectorize index '{Index}'...", index.IndexName);
            await apiClient.CreateVectorizeIndexAsync(token, accountId, new CreateVectorizeIndexRequest
            {
                Name = index.IndexName,
                Config = new VectorizeIndexConfig
                {
                    Dimensions = index.Dimensions,
                    Metric = index.Metric.ToApiValue(),
                },
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}
