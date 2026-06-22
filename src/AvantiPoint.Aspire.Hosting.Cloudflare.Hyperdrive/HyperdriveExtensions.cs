using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.Hyperdrive.Publishing;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Hyperdrive;

/// <summary>
/// Extension methods for provisioning a Cloudflare Hyperdrive configuration over an existing database.
/// The local dev loop is unchanged (your app talks to the database directly); at <c>aspire deploy</c> a
/// Hyperdrive config is created that points at the <b>production</b> database.
/// </summary>
public static class HyperdriveExtensions
{
    /// <summary>
    /// Publishes a Hyperdrive configuration whose production origin is the connection string of
    /// <paramref name="source"/> itself — use this when <paramref name="source"/> is an external
    /// connection-string resource (e.g. <c>builder.AddConnectionString("pg")</c>) that already resolves to
    /// the production database at deploy.
    /// </summary>
    public static IResourceBuilder<HyperdriveResource> PublishAsHyperdrive(
        this IResourceBuilder<IResourceWithConnectionString> source,
        string name)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.CreateHyperdrive(name, source.Resource.ConnectionStringExpression);
    }

    /// <summary>
    /// Publishes a Hyperdrive configuration whose production origin is supplied separately as a deployment
    /// parameter — the common case: <paramref name="source"/> is the local Aspire database used in dev, and
    /// <paramref name="productionConnectionString"/> holds the real production connection string (required at
    /// deploy, optional in dev via <c>AddDeploymentParameter</c>).
    /// </summary>
    public static IResourceBuilder<HyperdriveResource> PublishAsHyperdrive(
        this IResourceBuilder<IResourceWithConnectionString> source,
        string name,
        IResourceBuilder<ParameterResource> productionConnectionString)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(productionConnectionString);
        return source.CreateHyperdrive(name, ReferenceExpression.Create($"{productionConnectionString.Resource}"));
    }

    /// <summary>
    /// Publishes a Hyperdrive configuration whose production origin is another connection-string resource
    /// (e.g. an external production database modeled with <c>builder.AddConnectionString(...)</c>).
    /// </summary>
    public static IResourceBuilder<HyperdriveResource> PublishAsHyperdrive(
        this IResourceBuilder<IResourceWithConnectionString> source,
        string name,
        IResourceBuilder<IResourceWithConnectionString> productionConnectionSource)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(productionConnectionSource);
        return source.CreateHyperdrive(name, productionConnectionSource.Resource.ConnectionStringExpression);
    }

    /// <summary>Disables Hyperdrive's query caching for this configuration.</summary>
    public static IResourceBuilder<HyperdriveResource> WithCachingDisabled(this IResourceBuilder<HyperdriveResource> hyperdrive)
    {
        hyperdrive.Resource.CachingDisabled = true;
        return hyperdrive;
    }

    /// <summary>Permits <c>aspire deploy --destroy</c> to delete this Hyperdrive configuration. Off by default.</summary>
    public static IResourceBuilder<HyperdriveResource> AllowDeletion(this IResourceBuilder<HyperdriveResource> hyperdrive)
    {
        hyperdrive.Resource.AllowDestroy = true;
        return hyperdrive;
    }

    /// <summary>
    /// Binds a Hyperdrive configuration to a hand-authored Cloudflare Worker. At deploy the provisioned
    /// config id is surfaced with a <c>wrangler.jsonc</c> binding snippet (<paramref name="bindingName"/>)
    /// to add to the Worker's config — Workers consume Hyperdrive via a binding, which the integration does
    /// not own for hand-authored Workers.
    /// </summary>
    public static IResourceBuilder<CloudflareWorkerResource> WithHyperdrive(
        this IResourceBuilder<CloudflareWorkerResource> worker,
        IResourceBuilder<HyperdriveResource> hyperdrive,
        string bindingName = "HYPERDRIVE")
    {
        ArgumentNullException.ThrowIfNull(worker);
        ArgumentNullException.ThrowIfNull(hyperdrive);
        hyperdrive.Resource.Bindings.Add((worker.Resource.Name, bindingName));
        return worker;
    }

    private static IResourceBuilder<HyperdriveResource> CreateHyperdrive(
        this IResourceBuilder<IResourceWithConnectionString> source,
        string name,
        ReferenceExpression productionConnectionString)
    {
        var builder = source.ApplicationBuilder;
        var environment = builder.GetCloudflareEnvironment();
        environment.Resource.RequireScopes(CloudflareScopes.HyperdriveEdit);

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ICloudflarePublishTarget, HyperdrivePublishTarget>());

        var resource = new HyperdriveResource(name, environment.Resource, productionConnectionString);
        return builder.AddResource(resource);
    }
}
