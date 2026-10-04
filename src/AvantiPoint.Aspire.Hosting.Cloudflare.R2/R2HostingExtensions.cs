using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;
using AvantiPoint.Aspire.Hosting.Cloudflare.R2.Emulator;
using AvantiPoint.Aspire.Hosting.Cloudflare.R2.Provisioning;
using AvantiPoint.Aspire.Hosting.Cloudflare.R2.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.R2;

/// <summary>Extension methods for adding Cloudflare R2 buckets to an Aspire application.</summary>
public static class R2HostingExtensions
{
    /// <summary>Environment variable the real R2 S3 access key id is read from.</summary>
    public const string AccessKeyEnvVar = "R2_ACCESS_KEY_ID";

    /// <summary>Environment variable the real R2 S3 secret access key is read from.</summary>
    public const string SecretKeyEnvVar = "R2_SECRET_ACCESS_KEY";

    /// <summary>
    /// Adds an R2 bucket, using the single Cloudflare environment added to the application. Add one with
    /// <see cref="CloudflareEnvironmentExtensions.AddCloudflareEnvironment"/> first.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The Aspire resource name (also the connection name consumers reference).</param>
    /// <param name="bucketName">The R2 bucket name; defaults to <paramref name="name"/>.</param>
    [AspireExport("addR2Bucket")]
    public static IResourceBuilder<R2BucketResource> AddR2Bucket(
        this IDistributedApplicationBuilder builder,
        string name,
        string? bucketName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.GetCloudflareEnvironment().AddR2Bucket(name, bucketName);
    }

    /// <summary>
    /// Adds an R2 bucket to a specific Cloudflare environment. By default the bucket targets <b>real R2</b>
    /// (provisioned during <c>aspire run</c> and <c>aspire deploy</c>). Call <see cref="RunAsEmulator"/> to
    /// back it with the local MinIO S3 emulator during <c>aspire run</c> (a credential-free inner loop).
    /// </summary>
    /// <param name="environment">The Cloudflare environment builder.</param>
    /// <param name="name">The Aspire resource name (also the connection name consumers reference).</param>
    /// <param name="bucketName">The R2 bucket name; defaults to <paramref name="name"/>.</param>
    [AspireExport("addR2BucketInEnvironment")]
    public static IResourceBuilder<R2BucketResource> AddR2Bucket(
        this IResourceBuilder<CloudflareEnvironmentResource> environment,
        string name,
        string? bucketName = null)
    {
        ArgumentNullException.ThrowIfNull(environment);

        var builder = environment.ApplicationBuilder;
        environment.Resource.RequireScopes(CloudflareScopes.WorkersR2StorageEdit);

        // Register the R2 publish target so the environment's deploy pipeline provisions buckets.
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ICloudflarePublishTarget, R2PublishTarget>());

        var resource = new R2BucketResource(name, environment.Resource, bucketName ?? name);
        var bucket = builder.AddResource(resource);

        // Real R2 by default; RunAsEmulator() opts into MinIO for run mode.
        ConfigureReal(bucket);

        // In run mode the real bucket is provisioned on start (skipped for buckets switched to the emulator).
        if (builder.ExecutionContext.IsRunMode)
        {
            R2RealProvisioning.Register(builder, environment.Resource, resource);
        }

        return bucket;
    }

    /// <summary>Sets the R2 location hint (e.g. <c>weur</c>, <c>enam</c>) used when provisioning.</summary>
    [AspireExport("withLocationHint")]
    public static IResourceBuilder<R2BucketResource> WithLocationHint(
        this IResourceBuilder<R2BucketResource> bucket,
        string locationHint)
    {
        bucket.Resource.LocationHint = locationHint;
        return bucket;
    }

    /// <summary>
    /// Permits <c>aspire destroy</c> to delete this real R2 bucket. Off by default so a
    /// destroy never silently drops stored objects.
    /// </summary>
    [AspireExport("allowDeletion")]
    public static IResourceBuilder<R2BucketResource> AllowDeletion(this IResourceBuilder<R2BucketResource> bucket)
    {
        bucket.Resource.AllowDestroy = true;
        return bucket;
    }

    /// <summary>
    /// Backs this bucket with the local MinIO S3 emulator during <c>aspire run</c> (no Cloudflare
    /// credentials required). Ignored during <c>aspire publish</c>/<c>deploy</c>, which always use real R2.
    /// Mirrors the <c>RunAsEmulator()</c> convention of Aspire's Azure integrations.
    /// </summary>
    [AspireExport("runAsEmulator")]
    public static IResourceBuilder<R2BucketResource> RunAsEmulator(this IResourceBuilder<R2BucketResource> bucket)
    {
        ArgumentNullException.ThrowIfNull(bucket);

        if (!bucket.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            return bucket; // publish/deploy always uses real R2
        }

        var environment = bucket.ApplicationBuilder.CreateResourceBuilder(bucket.Resource.Environment);
        ConfigureEmulator(environment, bucket);
        return bucket;
    }

    private static void ConfigureEmulator(
        IResourceBuilder<CloudflareEnvironmentResource> environment,
        IResourceBuilder<R2BucketResource> bucket)
    {
        var minio = MinioEmulator.GetOrAdd(environment, bucket.Resource);
        var resource = bucket.Resource;
        resource.UseEmulator = true;
        resource.EmulatorEndpoint = minio.S3Endpoint;
        resource.EmulatorAccessKey = minio.AccessKey;
        resource.EmulatorSecretKey = minio.SecretKey;
        bucket.WaitFor(minio.Container);
    }

    private static void ConfigureReal(IResourceBuilder<R2BucketResource> bucket)
    {
        var builder = bucket.ApplicationBuilder;
        var resource = bucket.Resource;
        resource.UseEmulator = false;
        resource.RealAccessKey ??= GetOrAddSecretParameter(builder, "r2-access-key-id", AccessKeyEnvVar).Resource;
        resource.RealSecretKey ??= GetOrAddSecretParameter(builder, "r2-secret-access-key", SecretKeyEnvVar).Resource;
    }

    private static IResourceBuilder<ParameterResource> GetOrAddSecretParameter(
        IDistributedApplicationBuilder builder,
        string parameterName,
        string environmentVariable)
    {
        var existing = builder.Resources.OfType<ParameterResource>().FirstOrDefault(p => p.Name == parameterName);
        if (existing is not null)
        {
            return builder.CreateResourceBuilder(existing);
        }

        // Resolve lazily from env/config. Return empty (rather than throw) when unset so the
        // deployment-state save during publish doesn't warn for apps that don't access R2 at runtime;
        // a consuming app that actually needs the credentials will surface a clear S3 auth error.
        return builder.AddParameter(parameterName, () =>
            Environment.GetEnvironmentVariable(environmentVariable)
            ?? builder.Configuration[$"Parameters:{parameterName}"]
            ?? string.Empty,
            secret: true);
    }
}
