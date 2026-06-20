using System.Globalization;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AvantiPoint.Aspire.Hosting.Cloudflare;

/// <summary>Extension methods for adding hand-authored Cloudflare Workers to an Aspire application.</summary>
public static class CloudflareWorkerExtensions
{
    /// <summary>
    /// Adds a Cloudflare Worker from a project directory (containing its own wrangler config and source).
    /// During <c>aspire run</c> it runs locally with <c>wrangler dev</c> (Miniflare); during <c>aspire deploy</c>
    /// it is deployed with <c>wrangler deploy</c>. Bindings, routes and custom domains are managed in the
    /// Worker's own wrangler configuration.
    /// </summary>
    /// <param name="environment">The Cloudflare environment builder.</param>
    /// <param name="name">The Aspire resource name.</param>
    /// <param name="workerDirectory">Path to the Worker project (relative to the AppHost).</param>
    /// <param name="configure">Optional configuration.</param>
    public static IResourceBuilder<CloudflareWorkerResource> AddCloudflareWorker(
        this IResourceBuilder<CloudflareEnvironmentResource> environment,
        string name,
        string workerDirectory,
        Action<CloudflareWorkerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentException.ThrowIfNullOrEmpty(workerDirectory);

        var builder = environment.ApplicationBuilder;
        environment.Resource.RequireScopes(CloudflareScopes.WorkersScriptsEdit);
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ICloudflarePublishTarget, WorkerPublishTarget>());

        var options = new CloudflareWorkerOptions();
        configure?.Invoke(options);

        var fullDirectory = Path.GetFullPath(workerDirectory, builder.AppHostDirectory);

        // wrangler is an npm shim; on Windows it must be launched through cmd.
        var (command, prefixArgs) = OperatingSystem.IsWindows()
            ? ("cmd.exe", new[] { "/c", "wrangler" })
            : ("wrangler", []);

        var resource = new CloudflareWorkerResource(name, command, fullDirectory, environment.Resource);
        var worker = builder.AddResource(resource).ExcludeFromManifest();

        if (builder.ExecutionContext.IsRunMode)
        {
            var args = new List<string>(prefixArgs)
            {
                "dev",
                "--port", options.Port.ToString(CultureInfo.InvariantCulture),
                "--ip", "127.0.0.1",
            };

            worker.WithArgs(args.ToArray())
                  .WithHttpEndpoint(port: options.Port, name: "http", isProxied: false);
        }

        return worker;
    }
}
