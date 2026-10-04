using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.KV.Provisioning;
using AvantiPoint.Aspire.Hosting.Cloudflare.KV.Publishing;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.KV;

/// <summary>Extension methods for adding Cloudflare Workers KV namespaces to an Aspire application.</summary>
public static class KvHostingExtensions
{
    /// <summary>
    /// Adds a KV namespace, using the single Cloudflare environment added to the application. Add one with
    /// <see cref="CloudflareEnvironmentExtensions.AddCloudflareEnvironment"/> first.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The Aspire resource name (also the connection name consumers reference).</param>
    /// <param name="title">The KV namespace title; defaults to <paramref name="name"/>.</param>
    [AspireExport("addKvNamespace")]
    public static IResourceBuilder<KvNamespaceResource> AddKvNamespace(
        this IDistributedApplicationBuilder builder,
        string name,
        string? title = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.GetCloudflareEnvironment().AddKvNamespace(name, title);
    }

    /// <summary>
    /// Adds a KV namespace to a specific Cloudflare environment. By default it targets <b>real KV</b>
    /// (provisioned during <c>aspire run</c> and <c>aspire deploy</c>). Call <see cref="RunAsEmulator"/>
    /// to back it with an in-memory store during <c>aspire run</c>.
    /// </summary>
    /// <param name="environment">The Cloudflare environment builder.</param>
    /// <param name="name">The Aspire resource name (also the connection name consumers reference).</param>
    /// <param name="title">The KV namespace title; defaults to <paramref name="name"/>.</param>
    [AspireExport("addKvNamespaceInEnvironment")]
    public static IResourceBuilder<KvNamespaceResource> AddKvNamespace(
        this IResourceBuilder<CloudflareEnvironmentResource> environment,
        string name,
        string? title = null)
    {
        ArgumentNullException.ThrowIfNull(environment);

        var builder = environment.ApplicationBuilder;
        environment.Resource.RequireScopes(CloudflareScopes.WorkersKVEdit);

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ICloudflarePublishTarget, KvPublishTarget>());

        var resource = new KvNamespaceResource(name, environment.Resource, title ?? name);
        var ns = builder.AddResource(resource);

        if (builder.ExecutionContext.IsRunMode)
        {
            KvRealProvisioning.Register(builder, environment.Resource, resource);
        }

        return ns;
    }

    /// <summary>
    /// Backs this namespace with an in-process in-memory store during <c>aspire run</c> (no Cloudflare
    /// credentials required). Ignored during <c>aspire publish</c>/<c>deploy</c>, which always use real KV.
    /// </summary>
    [AspireExport("runAsEmulator")]
    public static IResourceBuilder<KvNamespaceResource> RunAsEmulator(this IResourceBuilder<KvNamespaceResource> ns)
    {
        ArgumentNullException.ThrowIfNull(ns);

        if (ns.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            ns.Resource.UseEmulator = true;
        }

        return ns;
    }

    /// <summary>Uses a specific API token (a parameter) for runtime KV access, instead of the environment's token.</summary>
    [AspireExport("withAccessToken")]
    public static IResourceBuilder<KvNamespaceResource> WithAccessToken(
        this IResourceBuilder<KvNamespaceResource> ns,
        IResourceBuilder<ParameterResource> token)
    {
        ns.Resource.AccessToken = token.Resource;
        return ns;
    }

    /// <summary>Permits <c>aspire destroy</c> to delete this real namespace. Off by default (data-loss guard).</summary>
    [AspireExport("allowDeletion")]
    public static IResourceBuilder<KvNamespaceResource> AllowDeletion(this IResourceBuilder<KvNamespaceResource> ns)
    {
        ns.Resource.AllowDestroy = true;
        return ns;
    }
}
