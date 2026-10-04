using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.D1.Provisioning;
using AvantiPoint.Aspire.Hosting.Cloudflare.D1.Publishing;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.D1;

/// <summary>Extension methods for adding Cloudflare D1 databases to an Aspire application.</summary>
public static class D1HostingExtensions
{
    /// <summary>
    /// Adds a D1 database, using the single Cloudflare environment added to the application. Add one with
    /// <see cref="CloudflareEnvironmentExtensions.AddCloudflareEnvironment"/> first.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The Aspire resource name (also the connection name consumers reference).</param>
    /// <param name="databaseName">The D1 database name; defaults to <paramref name="name"/>.</param>
    [AspireExport("addD1Database")]
    public static IResourceBuilder<D1DatabaseResource> AddD1Database(
        this IDistributedApplicationBuilder builder,
        string name,
        string? databaseName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.GetCloudflareEnvironment().AddD1Database(name, databaseName);
    }

    /// <summary>
    /// Adds a D1 database to a specific Cloudflare environment. By default the database targets <b>real
    /// D1</b> (provisioned during <c>aspire run</c> and <c>aspire deploy</c>). Call <see cref="RunAsEmulator"/>
    /// to back it with a local SQLite file during <c>aspire run</c> (a credential-free inner loop).
    /// </summary>
    /// <param name="environment">The Cloudflare environment builder.</param>
    /// <param name="name">The Aspire resource name (also the connection name consumers reference).</param>
    /// <param name="databaseName">The D1 database name; defaults to <paramref name="name"/>.</param>
    [AspireExport("addD1DatabaseInEnvironment")]
    public static IResourceBuilder<D1DatabaseResource> AddD1Database(
        this IResourceBuilder<CloudflareEnvironmentResource> environment,
        string name,
        string? databaseName = null)
    {
        ArgumentNullException.ThrowIfNull(environment);

        var builder = environment.ApplicationBuilder;
        environment.Resource.RequireScopes(CloudflareScopes.D1Edit);

        // Register the D1 publish target so the environment's deploy pipeline provisions databases.
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ICloudflarePublishTarget, D1PublishTarget>());

        var resource = new D1DatabaseResource(name, environment.Resource, databaseName ?? name);
        var database = builder.AddResource(resource);

        // In run mode the real database is provisioned on start (skipped for databases switched to the emulator).
        if (builder.ExecutionContext.IsRunMode)
        {
            D1RealProvisioning.Register(builder, environment.Resource, resource);
        }

        return database;
    }

    /// <summary>
    /// Backs this database with a local SQLite file during <c>aspire run</c> (no Cloudflare credentials
    /// required). Ignored during <c>aspire publish</c>/<c>deploy</c>, which always use real D1. Mirrors the
    /// <c>RunAsEmulator()</c> convention of Aspire's Azure integrations.
    /// </summary>
    [AspireExport("runAsEmulator")]
    public static IResourceBuilder<D1DatabaseResource> RunAsEmulator(this IResourceBuilder<D1DatabaseResource> database)
    {
        ArgumentNullException.ThrowIfNull(database);

        if (!database.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            return database; // publish/deploy always uses real D1
        }

        var resource = database.Resource;
        resource.UseEmulator = true;

        var directory = Path.Combine(database.ApplicationBuilder.AppHostDirectory, ".aspire", "d1");
        Directory.CreateDirectory(directory);
        resource.SqliteFilePath = Path.Combine(directory, $"{resource.DatabaseName}.db");

        return database;
    }

    /// <summary>Sets the D1 primary location hint (e.g. <c>weur</c>, <c>enam</c>) used when provisioning.</summary>
    [AspireExport("withLocationHint")]
    public static IResourceBuilder<D1DatabaseResource> WithLocationHint(
        this IResourceBuilder<D1DatabaseResource> database,
        string locationHint)
    {
        database.Resource.PrimaryLocationHint = locationHint;
        return database;
    }

    /// <summary>
    /// Uses a specific API token (a parameter) for runtime D1 data access, instead of the environment's
    /// Cloudflare API token. Useful to scope runtime access to just D1.
    /// </summary>
    [AspireExport("withAccessToken")]
    public static IResourceBuilder<D1DatabaseResource> WithAccessToken(
        this IResourceBuilder<D1DatabaseResource> database,
        IResourceBuilder<ParameterResource> token)
    {
        database.Resource.AccessToken = token.Resource;
        return database;
    }

    /// <summary>
    /// Permits <c>aspire deploy --destroy</c> to delete this real D1 database. Off by default so a
    /// destroy never silently drops stored data.
    /// </summary>
    [AspireExport("allowDeletion")]
    public static IResourceBuilder<D1DatabaseResource> AllowDeletion(this IResourceBuilder<D1DatabaseResource> database)
    {
        database.Resource.AllowDestroy = true;
        return database;
    }
}
