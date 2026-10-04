using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;
using AvantiPoint.Aspire.Hosting.Cloudflare.Queues.Provisioning;
using AvantiPoint.Aspire.Hosting.Cloudflare.Queues.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Queues;

/// <summary>Extension methods for adding Cloudflare Queues to an Aspire application.</summary>
public static class QueuesHostingExtensions
{
    /// <summary>
    /// Adds a Queue, using the single Cloudflare environment added to the application. Add one with
    /// <see cref="CloudflareEnvironmentExtensions.AddCloudflareEnvironment"/> first.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The Aspire resource name (also the connection name consumers reference).</param>
    /// <param name="queueName">The Queue name; defaults to <paramref name="name"/>.</param>
    [AspireExport("addQueue")]
    public static IResourceBuilder<QueueResource> AddQueue(
        this IDistributedApplicationBuilder builder,
        string name,
        string? queueName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.GetCloudflareEnvironment().AddQueue(name, queueName);
    }

    /// <summary>
    /// Adds a Queue to a specific Cloudflare environment. By default it targets <b>real Queues</b>
    /// (provisioned during <c>aspire run</c> and <c>aspire deploy</c>). Call <see cref="RunAsEmulator"/>
    /// to back it with an in-memory queue during <c>aspire run</c>.
    /// </summary>
    /// <param name="environment">The Cloudflare environment builder.</param>
    /// <param name="name">The Aspire resource name (also the connection name consumers reference).</param>
    /// <param name="queueName">The Queue name; defaults to <paramref name="name"/>.</param>
    [AspireExport("addQueueInEnvironment")]
    public static IResourceBuilder<QueueResource> AddQueue(
        this IResourceBuilder<CloudflareEnvironmentResource> environment,
        string name,
        string? queueName = null)
    {
        ArgumentNullException.ThrowIfNull(environment);

        var builder = environment.ApplicationBuilder;
        environment.Resource.RequireScopes(CloudflareScopes.QueuesEdit);

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ICloudflarePublishTarget, QueuePublishTarget>());

        var resource = new QueueResource(name, environment.Resource, queueName ?? name);
        var queue = builder.AddResource(resource);

        if (builder.ExecutionContext.IsRunMode)
        {
            QueueRealProvisioning.Register(builder, environment.Resource, resource);
        }

        return queue;
    }

    /// <summary>
    /// Backs this queue with an in-process in-memory queue during <c>aspire run</c> (no Cloudflare
    /// credentials required). Ignored during <c>aspire publish</c>/<c>deploy</c>, which always use real Queues.
    /// </summary>
    [AspireExport("runAsEmulator")]
    public static IResourceBuilder<QueueResource> RunAsEmulator(this IResourceBuilder<QueueResource> queue)
    {
        ArgumentNullException.ThrowIfNull(queue);

        if (queue.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            queue.Resource.UseEmulator = true;
        }

        return queue;
    }

    /// <summary>Uses a specific API token (a parameter) for runtime Queue access, instead of the environment's token.</summary>
    [AspireExport("withAccessToken")]
    public static IResourceBuilder<QueueResource> WithAccessToken(
        this IResourceBuilder<QueueResource> queue,
        IResourceBuilder<ParameterResource> token)
    {
        queue.Resource.AccessToken = token.Resource;
        return queue;
    }

    /// <summary>Permits <c>aspire deploy --destroy</c> to delete this real queue. Off by default (data-loss guard).</summary>
    [AspireExport("allowDeletion")]
    public static IResourceBuilder<QueueResource> AllowDeletion(this IResourceBuilder<QueueResource> queue)
    {
        queue.Resource.AllowDestroy = true;
        return queue;
    }
}
