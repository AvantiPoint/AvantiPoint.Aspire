using System.Text;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.JavaScript;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.Pages.Publishing;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Pages;

/// <summary>Extension methods for deploying Aspire JavaScript apps to Cloudflare Pages.</summary>
public static class CloudflarePagesExtensions
{
    /// <summary>
    /// Deploys this JavaScript app to Cloudflare Pages, using the single Cloudflare environment added to
    /// the application. Add one with <c>AddCloudflareEnvironment</c> first.
    /// </summary>
    public static IResourceBuilder<T> PublishAsCloudflarePages<T>(
        this IResourceBuilder<T> app,
        Action<CloudflarePagesOptions>? configure = null)
        where T : JavaScriptAppResource
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.PublishAsCloudflarePages(app.ApplicationBuilder.GetCloudflareEnvironment(), configure);
    }

    /// <summary>
    /// Deploys this JavaScript app (e.g. created with <c>AddViteApp</c>/<c>AddNodeApp</c>) to a specific
    /// Cloudflare environment during <c>aspire deploy</c>. The app's build is run during publish and the
    /// output directory is uploaded with <c>wrangler pages deploy</c>.
    /// </summary>
    public static IResourceBuilder<T> PublishAsCloudflarePages<T>(
        this IResourceBuilder<T> app,
        IResourceBuilder<CloudflareEnvironmentResource> environment,
        Action<CloudflarePagesOptions>? configure = null)
        where T : JavaScriptAppResource
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(environment);

        var builder = app.ApplicationBuilder;
        environment.Resource.RequireScopes(CloudflareScopes.PagesEdit);
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ICloudflarePublishTarget, PagesPublishTarget>());

        var options = new CloudflarePagesOptions();
        configure?.Invoke(options);

        app.Resource.Annotations.Add(new CloudflarePagesAnnotation(environment.Resource)
        {
            ProjectName = options.ProjectName ?? SanitizeProjectName(app.Resource.Name),
            OutputDirectory = options.OutputDirectory,
            Branch = options.Branch,
            BuildCommand = options.BuildCommand,
            SkipBuild = options.SkipBuild,
            WorkingDirectory = (app.Resource as ExecutableResource)?.WorkingDirectory,
        });

        return app;
    }

    /// <summary>
    /// Cloudflare Pages project names must be lowercase, alphanumeric or hyphen, and at most 58 chars.
    /// </summary>
    internal static string SanitizeProjectName(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (var ch in name.ToLowerInvariant())
        {
            sb.Append(char.IsAsciiLetterOrDigit(ch) ? ch : '-');
        }

        var sanitized = sb.ToString().Trim('-');
        if (sanitized.Length > 58)
        {
            sanitized = sanitized[..58].TrimEnd('-');
        }

        return sanitized.Length == 0 ? "app" : sanitized;
    }
}
