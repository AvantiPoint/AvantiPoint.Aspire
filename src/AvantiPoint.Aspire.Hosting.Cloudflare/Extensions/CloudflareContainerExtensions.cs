using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AvantiPoint.Aspire.Hosting.Cloudflare;

/// <summary>Extension methods for deploying a .NET project as a Cloudflare Container.</summary>
public static class CloudflareContainerExtensions
{
    /// <summary>
    /// Deploys this project as a Cloudflare Container during <c>aspire deploy</c>. A container image is
    /// built from the project, fronted by a Worker + Durable Object, and deployed with <c>wrangler</c>.
    /// During <c>aspire run</c> the project runs locally as usual.
    /// </summary>
    /// <summary>
    /// Deploys this project as a Cloudflare Container, using the single Cloudflare environment added to
    /// the application. Add one with <see cref="CloudflareEnvironmentExtensions.AddCloudflareEnvironment"/> first.
    /// </summary>
    public static IResourceBuilder<ProjectResource> PublishAsCloudflareContainer(
        this IResourceBuilder<ProjectResource> project,
        Action<CloudflareContainerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        return project.PublishAsCloudflareContainer(project.ApplicationBuilder.GetCloudflareEnvironment(), configure);
    }

    /// <summary>
    /// Deploys this project as a Cloudflare Container into a specific environment. Use this overload when
    /// the application has more than one Cloudflare environment.
    /// </summary>
    public static IResourceBuilder<ProjectResource> PublishAsCloudflareContainer(
        this IResourceBuilder<ProjectResource> project,
        IResourceBuilder<CloudflareEnvironmentResource> environment,
        Action<CloudflareContainerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(environment);

        var builder = project.ApplicationBuilder;
        environment.Resource.RequireScopes(CloudflareScopes.WorkersScriptsEdit);
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ICloudflarePublishTarget, ContainerPublishTarget>());

        var options = new CloudflareContainerOptions();
        configure?.Invoke(options);

        var workerName = options.WorkerName ?? CloudflareNaming.WorkerName(project.Resource.Name);
        var className = CloudflareNaming.ClassName(project.Resource.Name);

        project.Resource.Annotations.Add(new CloudflareContainerAnnotation(environment.Resource)
        {
            WorkerName = workerName,
            ClassName = className,
            BindingName = CloudflareNaming.BindingName(className),
            Port = options.Port,
            MaxInstances = options.MaxInstances,
            InstanceType = options.InstanceType,
            SleepAfter = options.SleepAfter,
            CompatibilityDate = options.CompatibilityDate,
        });

        return project;
    }
}
