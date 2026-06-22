using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.KV.Provisioning;

/// <summary>
/// Tracks KV namespaces that target real Cloudflare KV during <c>aspire run</c> (the default), and
/// provisions them once before the app starts. Namespaces switched to the in-memory emulator with
/// <c>RunAsEmulator()</c> are skipped.
/// </summary>
internal sealed class KvRealProvisioningAnnotation : IResourceAnnotation
{
    public List<KvNamespaceResource> Namespaces { get; } = [];
}

internal static class KvRealProvisioning
{
    public static void Register(
        IDistributedApplicationBuilder builder,
        CloudflareEnvironmentResource environment,
        KvNamespaceResource ns)
    {
        if (!environment.TryGetLastAnnotation<KvRealProvisioningAnnotation>(out var annotation))
        {
            annotation = new KvRealProvisioningAnnotation();
            environment.Annotations.Add(annotation);

            builder.Eventing.Subscribe<BeforeStartEvent>((evt, ct) =>
                ProvisionAsync(environment, annotation, evt.Services, ct));
        }

        if (!annotation.Namespaces.Contains(ns))
        {
            annotation.Namespaces.Add(ns);
        }
    }

    private static async Task ProvisionAsync(
        CloudflareEnvironmentResource environment,
        KvRealProvisioningAnnotation annotation,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var realNamespaces = annotation.Namespaces.Where(n => !n.UseEmulator).ToList();
        if (realNamespaces.Count == 0)
        {
            return;
        }

        var logger = services.GetService<ILoggerFactory>()?.CreateLogger("Cloudflare.KV.Provisioning");
        var apiClient = services.GetRequiredService<ICloudflareApiClient>();
        var validator = services.GetRequiredService<CloudflareTokenValidator>();

        var token = await environment.ApiToken.GetValueAsync(cancellationToken).ConfigureAwait(false) ?? string.Empty;
        var accountId = await environment.AccountId.GetValueAsync(cancellationToken).ConfigureAwait(false) ?? string.Empty;

        await validator.ValidateAsync(token, environment.RequiredScopes, cancellationToken).ConfigureAwait(false);

        foreach (var ns in realNamespaces)
        {
            logger?.LogInformation("Provisioning real KV namespace '{Namespace}'...", ns.Title);
            await apiClient.CreateKvNamespaceAsync(token, accountId, new CreateKvNamespaceRequest { Title = ns.Title }, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
