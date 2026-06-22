using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.D1.Provisioning;

/// <summary>
/// Tracks D1 databases that target real Cloudflare D1 during <c>aspire run</c> (the default), and
/// provisions them once before the app starts. Databases switched to the local SQLite emulator with
/// <c>RunAsEmulator()</c> are skipped.
/// </summary>
internal sealed class D1RealProvisioningAnnotation : IResourceAnnotation
{
    public List<D1DatabaseResource> Databases { get; } = [];
}

internal static class D1RealProvisioning
{
    /// <summary>Registers <paramref name="database"/> for real provisioning and ensures the
    /// before-start provisioner is subscribed exactly once for the environment.</summary>
    public static void Register(
        IDistributedApplicationBuilder builder,
        CloudflareEnvironmentResource environment,
        D1DatabaseResource database)
    {
        if (!environment.TryGetLastAnnotation<D1RealProvisioningAnnotation>(out var annotation))
        {
            annotation = new D1RealProvisioningAnnotation();
            environment.Annotations.Add(annotation);

            builder.Eventing.Subscribe<BeforeStartEvent>((evt, ct) =>
                ProvisionAsync(environment, annotation, evt.Services, ct));
        }

        if (!annotation.Databases.Contains(database))
        {
            annotation.Databases.Add(database);
        }
    }

    private static async Task ProvisionAsync(
        CloudflareEnvironmentResource environment,
        D1RealProvisioningAnnotation annotation,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        // Databases switched to the emulator with RunAsEmulator() are backed by SQLite — skip them.
        var realDatabases = annotation.Databases.Where(d => !d.UseEmulator).ToList();
        if (realDatabases.Count == 0)
        {
            return;
        }

        var logger = services.GetService<ILoggerFactory>()?.CreateLogger("Cloudflare.D1.Provisioning");
        var apiClient = services.GetRequiredService<ICloudflareApiClient>();
        var validator = services.GetRequiredService<CloudflareTokenValidator>();

        var token = await environment.ApiToken.GetValueAsync(cancellationToken).ConfigureAwait(false) ?? string.Empty;
        var accountId = await environment.AccountId.GetValueAsync(cancellationToken).ConfigureAwait(false) ?? string.Empty;

        await validator.ValidateAsync(token, environment.RequiredScopes, cancellationToken).ConfigureAwait(false);

        foreach (var database in realDatabases)
        {
            logger?.LogInformation("Provisioning real D1 database '{Database}'...", database.DatabaseName);
            await apiClient.CreateD1DatabaseAsync(token, accountId, new CreateD1DatabaseRequest
            {
                Name = database.DatabaseName,
                PrimaryLocationHint = database.PrimaryLocationHint,
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}
