using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api;
using AvantiPoint.Aspire.Hosting.Cloudflare.Cli;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Pages.Publishing;

/// <summary>
/// Publish target for Cloudflare Pages. During publish it runs the JS app's build; during deploy it
/// uploads the build output with <c>wrangler pages deploy</c>, creating the Pages project if needed.
/// </summary>
internal sealed class PagesPublishTarget(ILogger<PagesPublishTarget> logger, IWranglerCli wrangler) : ICloudflarePublishTarget
{
    public bool CanHandle(IResource resource) => resource.Annotations.OfType<CloudflarePagesAnnotation>().Any();

    public async Task GenerateArtifactsAsync(CloudflarePublishContext context, IResource resource, CancellationToken cancellationToken)
    {
        var annotation = GetAnnotation(resource);
        if (annotation.SkipBuild)
        {
            return;
        }

        var environment = await PagesBuildEnvironment.ResolveAsync(resource, logger, cancellationToken).ConfigureAwait(false);
        var (file, args) = SplitCommand(annotation.BuildCommand);
        logger.LogInformation("Building Pages app '{Resource}' with '{Command}'...", resource.Name, annotation.BuildCommand);

        var result = await CliRunner.RunAsync(file, args, annotation.WorkingDirectory, environment: environment, logger: logger, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"Pages build '{annotation.BuildCommand}' failed for '{resource.Name}' (exit {result.ExitCode}).\n{result.StandardError}");
        }
    }

    public async Task DeployAsync(CloudflareDeployContext context, IResource resource, CancellationToken cancellationToken)
    {
        var annotation = GetAnnotation(resource);

        await EnsureProjectAsync(context, annotation, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Deploying '{Project}' to Cloudflare Pages...", annotation.ProjectName);
        await wrangler.RunAsync(
            ["pages", "deploy", annotation.OutputDirectory, "--project-name", annotation.ProjectName, "--branch", annotation.Branch],
            workingDirectory: annotation.WorkingDirectory,
            apiToken: context.ApiToken,
            accountId: context.AccountId,
            logger: logger,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Deployed '{Project}' to Cloudflare Pages.", annotation.ProjectName);

        // Attach any custom domains: register with the Pages project and upsert a proxied CNAME.
        var customDomains = resource.Annotations.OfType<CustomDomainAnnotation>().ToList();
        if (customDomains.Count > 0)
        {
            var apiClient = context.Services.GetRequiredService<ICloudflareApiClient>();
            foreach (var domain in customDomains)
            {
                var zoneId = await domain.GetZoneIdAsync(cancellationToken).ConfigureAwait(false);
                var hostname = await domain.GetHostnameAsync(cancellationToken).ConfigureAwait(false);
                await apiClient.AttachPagesDomainAsync(context.ApiToken, context.AccountId, annotation.ProjectName, hostname, cancellationToken).ConfigureAwait(false);
                await apiClient.UpsertCnameRecordAsync(context.ApiToken, zoneId, hostname, $"{annotation.ProjectName}.pages.dev", cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task DestroyAsync(CloudflareDeployContext context, IResource resource, CancellationToken cancellationToken)
    {
        var annotation = GetAnnotation(resource);
        await wrangler.RunAsync(
            ["pages", "project", "delete", annotation.ProjectName, "--yes"],
            workingDirectory: annotation.WorkingDirectory,
            apiToken: context.ApiToken,
            accountId: context.AccountId,
            logger: logger,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureProjectAsync(CloudflareDeployContext context, CloudflarePagesAnnotation annotation, CancellationToken cancellationToken)
    {
        try
        {
            await wrangler.RunAsync(
                ["pages", "project", "create", annotation.ProjectName, "--production-branch", annotation.Branch],
                workingDirectory: annotation.WorkingDirectory,
                apiToken: context.ApiToken,
                accountId: context.AccountId,
                logger: logger,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already", StringComparison.OrdinalIgnoreCase))
        {
            // Project already exists — fine, continue to deploy.
            logger.LogInformation("Cloudflare Pages project '{Project}' already exists.", annotation.ProjectName);
        }
    }

    private static CloudflarePagesAnnotation GetAnnotation(IResource resource)
        => resource.Annotations.OfType<CloudflarePagesAnnotation>().Last();

    /// <summary>Splits a shell-style command into the executable and its arguments (whitespace-separated).</summary>
    internal static (string File, string[] Args) SplitCommand(string command)
    {
        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0
            ? ("npm", ["run", "build"])
            : (parts[0], parts[1..]);
    }
}
