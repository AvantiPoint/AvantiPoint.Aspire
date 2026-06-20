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
    /// Adds an R2 bucket to the Cloudflare environment. During <c>aspire run</c> the bucket is
    /// backed by a local MinIO S3 emulator (no Cloudflare credentials required); during
    /// <c>aspire publish</c>/<c>deploy</c> it is provisioned against the real Cloudflare account.
    /// Call <see cref="RunAsReal"/> to use a real R2 bucket during <c>aspire run</c> as well.
    /// </summary>
    /// <param name="environment">The Cloudflare environment builder.</param>
    /// <param name="name">The Aspire resource name (also the connection name consumers reference).</param>
    /// <param name="bucketName">The R2 bucket name; defaults to <paramref name="name"/>.</param>
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

        if (builder.ExecutionContext.IsRunMode)
        {
            ConfigureEmulator(environment, bucket);
        }
        else
        {
            ConfigureReal(bucket);
        }

        return bucket;
    }

    /// <summary>Sets the R2 location hint (e.g. <c>weur</c>, <c>enam</c>) used when provisioning.</summary>
    public static IResourceBuilder<R2BucketResource> WithLocationHint(
        this IResourceBuilder<R2BucketResource> bucket,
        string locationHint)
    {
        bucket.Resource.LocationHint = locationHint;
        return bucket;
    }

    /// <summary>
    /// Permits <c>aspire deploy --destroy</c> to delete this real R2 bucket. Off by default so a
    /// destroy never silently drops stored objects.
    /// </summary>
    public static IResourceBuilder<R2BucketResource> AllowDeletion(this IResourceBuilder<R2BucketResource> bucket)
    {
        bucket.Resource.AllowDestroy = true;
        return bucket;
    }

    /// <summary>
    /// Provisions and uses a real Cloudflare R2 bucket during <c>aspire run</c> instead of the local
    /// MinIO emulator. Requires a valid API token, account id and R2 S3 credentials
    /// (<c>R2_ACCESS_KEY_ID</c> / <c>R2_SECRET_ACCESS_KEY</c>).
    /// </summary>
    public static IResourceBuilder<R2BucketResource> RunAsReal(this IResourceBuilder<R2BucketResource> bucket)
    {
        if (!bucket.Resource.UseEmulator && bucket.Resource.RealAccessKey is not null)
        {
            return bucket; // already real
        }

        ConfigureReal(bucket);

        if (bucket.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            R2RealProvisioning.Register(bucket.ApplicationBuilder, bucket.Resource.Environment, bucket.Resource);
        }

        return bucket;
    }

    private static void ConfigureEmulator(
        IResourceBuilder<CloudflareEnvironmentResource> environment,
        IResourceBuilder<R2BucketResource> bucket)
    {
        var minio = MinioEmulator.GetOrAdd(environment, bucket.Resource.BucketName);
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

        return builder.AddParameter(parameterName, () =>
            Environment.GetEnvironmentVariable(environmentVariable)
            ?? builder.Configuration[$"Parameters:{parameterName}"]
            ?? throw new InvalidOperationException(
                $"R2 credential '{parameterName}' is not set. Provide it via the {environmentVariable} " +
                $"environment variable, user-secrets, or configuration key 'Parameters:{parameterName}'."),
            secret: true);
    }
}
