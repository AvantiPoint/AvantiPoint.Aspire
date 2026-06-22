using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api;
using AvantiPoint.Aspire.Hosting.Cloudflare.Cli;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing.Generators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;

/// <summary>
/// Publishes a .NET project as a Cloudflare Container. During publish it builds the app and generates
/// the Dockerfile, Worker (Durable Object) shim and <c>wrangler.jsonc</c>; during deploy it runs
/// <c>wrangler deploy</c>, which builds the image, pushes it to Cloudflare's registry and deploys the Worker.
/// </summary>
internal sealed class ContainerPublishTarget(ILogger<ContainerPublishTarget> logger, IWranglerCli wrangler) : ICloudflarePublishTarget
{
    public bool CanHandle(IResource resource) => resource.Annotations.OfType<CloudflareContainerAnnotation>().Any();

    public async Task GenerateArtifactsAsync(CloudflarePublishContext context, IResource resource, CancellationToken cancellationToken)
    {
        var annotation = GetAnnotation(resource);

        if (!resource.TryGetLastAnnotation<IProjectMetadata>(out var project))
        {
            throw new InvalidOperationException($"Resource '{resource.Name}' is not a project; cannot build a container image.");
        }

        var projectPath = project.ProjectPath;
        var projectDir = Path.GetDirectoryName(projectPath)!;
        var assemblyName = Path.GetFileNameWithoutExtension(projectPath);

        var workingDirectory = Path.Combine(projectDir, "obj", "cloudflare");
        var appDirectory = Path.Combine(workingDirectory, "app");
        annotation.WorkingDirectory = workingDirectory;

        if (Directory.Exists(appDirectory))
        {
            Directory.Delete(appDirectory, recursive: true);
        }

        Directory.CreateDirectory(workingDirectory);

        // 1. Build the app into <workdir>/app (the Dockerfile copies this in).
        logger.LogInformation("Publishing project '{Project}' for the Cloudflare Container...", resource.Name);
        var publish = await CliRunner.RunAsync(
            "dotnet",
            ["publish", projectPath, "-c", "Release", "--nologo", "-o", appDirectory],
            logger: logger,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!publish.Success)
        {
            throw new InvalidOperationException($"dotnet publish failed for '{resource.Name}' (exit {publish.ExitCode}).\n{publish.StandardError}");
        }

        // 2. Resolve the app's environment (connection strings, WithEnvironment) so the container receives it.
        var environment = await ContainerEnvironment.ResolveAsync(resource, logger).ConfigureAwait(false);
        logger.LogInformation("Injecting {Count} environment variable(s) into container '{Worker}'.", environment.Count, annotation.WorkerName);

        // 3. Generate the Dockerfile, Worker shim, package.json and wrangler.jsonc.
        await File.WriteAllTextAsync(Path.Combine(workingDirectory, "Dockerfile"),
            DockerfileGenerator.Generate(assemblyName, annotation.Port), cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(workingDirectory, "worker.js"),
            WorkerShimGenerator.GenerateWorker(annotation.ClassName, annotation.BindingName, annotation.Port, annotation.SleepAfter, environment), cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(workingDirectory, "package.json"),
            WorkerShimGenerator.GeneratePackageJson(annotation.WorkerName), cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(workingDirectory, "wrangler.jsonc"),
            WranglerContainerConfigGenerator.Generate(annotation), cancellationToken).ConfigureAwait(false);

        // 4. Install the Worker's dependencies (@cloudflare/containers) so wrangler can bundle it.
        var npm = await CliRunner.RunAsync("npm", ["install"], workingDirectory, logger: logger, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!npm.Success)
        {
            throw new InvalidOperationException($"npm install failed for '{resource.Name}' (exit {npm.ExitCode}).\n{npm.StandardError}");
        }
    }

    public async Task DeployAsync(CloudflareDeployContext context, IResource resource, CancellationToken cancellationToken)
    {
        var annotation = GetAnnotation(resource);
        var workingDirectory = annotation.WorkingDirectory
            ?? throw new InvalidOperationException($"Container artifacts for '{resource.Name}' were not generated. Run the publish step first.");

        logger.LogInformation("Deploying container Worker '{Worker}' to Cloudflare...", annotation.WorkerName);
        await wrangler.RunAsync(
            ["deploy"],
            workingDirectory: workingDirectory,
            apiToken: context.ApiToken,
            accountId: context.AccountId,
            logger: logger,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Deployed container Worker '{Worker}'.", annotation.WorkerName);

        // Attach any custom domains (Workers custom domains; Cloudflare manages DNS + SSL).
        var customDomains = resource.Annotations.OfType<CustomDomainAnnotation>().ToList();
        if (customDomains.Count > 0)
        {
            var apiClient = context.Services.GetRequiredService<ICloudflareApiClient>();
            foreach (var domain in customDomains)
            {
                var zoneId = await domain.GetZoneIdAsync(cancellationToken).ConfigureAwait(false);
                var hostname = await domain.GetHostnameAsync(cancellationToken).ConfigureAwait(false);
                await apiClient.AttachWorkersCustomDomainAsync(
                    context.ApiToken, context.AccountId, zoneId, hostname, annotation.WorkerName,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task DestroyAsync(CloudflareDeployContext context, IResource resource, CancellationToken cancellationToken)
    {
        var annotation = GetAnnotation(resource);
        await wrangler.RunAsync(
            ["delete", "--name", annotation.WorkerName],
            workingDirectory: annotation.WorkingDirectory,
            apiToken: context.ApiToken,
            accountId: context.AccountId,
            logger: logger,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static CloudflareContainerAnnotation GetAnnotation(IResource resource)
        => resource.Annotations.OfType<CloudflareContainerAnnotation>().Last();
}
