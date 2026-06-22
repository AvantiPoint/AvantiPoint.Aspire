using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Queues.Provisioning;

/// <summary>
/// Tracks Queues that target real Cloudflare during <c>aspire run</c> (the default), and provisions them
/// once before the app starts. Queues switched to the in-memory emulator with <c>RunAsEmulator()</c> are
/// skipped.
/// </summary>
internal sealed class QueueRealProvisioningAnnotation : IResourceAnnotation
{
    public List<QueueResource> Queues { get; } = [];
}

internal static class QueueRealProvisioning
{
    public static void Register(
        IDistributedApplicationBuilder builder,
        CloudflareEnvironmentResource environment,
        QueueResource queue)
    {
        if (!environment.TryGetLastAnnotation<QueueRealProvisioningAnnotation>(out var annotation))
        {
            annotation = new QueueRealProvisioningAnnotation();
            environment.Annotations.Add(annotation);

            builder.Eventing.Subscribe<BeforeStartEvent>((evt, ct) =>
                ProvisionAsync(environment, annotation, evt.Services, ct));
        }

        if (!annotation.Queues.Contains(queue))
        {
            annotation.Queues.Add(queue);
        }
    }

    private static async Task ProvisionAsync(
        CloudflareEnvironmentResource environment,
        QueueRealProvisioningAnnotation annotation,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var realQueues = annotation.Queues.Where(q => !q.UseEmulator).ToList();
        if (realQueues.Count == 0)
        {
            return;
        }

        var logger = services.GetService<ILoggerFactory>()?.CreateLogger("Cloudflare.Queues.Provisioning");
        var apiClient = services.GetRequiredService<ICloudflareApiClient>();
        var validator = services.GetRequiredService<CloudflareTokenValidator>();

        var token = await environment.ApiToken.GetValueAsync(cancellationToken).ConfigureAwait(false) ?? string.Empty;
        var accountId = await environment.AccountId.GetValueAsync(cancellationToken).ConfigureAwait(false) ?? string.Empty;

        await validator.ValidateAsync(token, environment.RequiredScopes, cancellationToken).ConfigureAwait(false);

        foreach (var queue in realQueues)
        {
            logger?.LogInformation("Provisioning real Queue '{Queue}'...", queue.QueueName);
            await apiClient.CreateQueueAsync(token, accountId, new CreateQueueRequest { QueueName = queue.QueueName }, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
