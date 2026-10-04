using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.R2.Emulator;

/// <summary>
/// Annotation placed on the <see cref="CloudflareEnvironmentResource"/> that holds the single shared
/// MinIO emulator used to back R2 buckets during <c>aspire run</c>, plus the set of buckets to
/// create once it is ready.
/// </summary>
internal sealed class MinioEmulatorAnnotation(
    IResourceBuilder<ContainerResource> container,
    EndpointReference s3Endpoint,
    string accessKey,
    string secretKey) : IResourceAnnotation
{
    public IResourceBuilder<ContainerResource> Container { get; } = container;
    public EndpointReference S3Endpoint { get; } = s3Endpoint;
    public string AccessKey { get; } = accessKey;
    public string SecretKey { get; } = secretKey;
    public List<R2BucketResource> Buckets { get; } = [];
}

internal static class MinioEmulator
{
    // MinIO root credentials for the local emulator (local-only; never used against real R2).
    public const string DefaultAccessKey = "cloudflare-r2-local";
    public const string DefaultSecretKey = "cloudflare-r2-local-secret";

    // Community build of the upstream MinIO release; upstream no longer distributes images.
    // Pin the multi-platform manifest so local emulation remains reproducible.
    private const string Image = "coollabsio/minio";
    private const string Tag = "RELEASE.2025-10-15T17-29-55Z";
    private const string ImageDigest = "69b55a1c1c5dc285ce04db96689f5b2102317fc77a50680a1874ca6efd1c87f9";
    private const int ApiPort = 9000;
    private const int ConsolePort = 9001;
    private const string S3EndpointName = "s3";

    /// <summary>
    /// Returns the shared MinIO emulator for the environment, creating it (and wiring bucket
    /// bootstrap) on first use. Tracks <paramref name="bucket"/> so it can be created and marked
    /// running when the emulator becomes ready.
    /// </summary>
    public static MinioEmulatorAnnotation GetOrAdd(
        IResourceBuilder<CloudflareEnvironmentResource> environment,
        R2BucketResource bucket)
    {
        if (environment.Resource.TryGetLastAnnotation<MinioEmulatorAnnotation>(out var existing))
        {
            existing.Buckets.Add(bucket);
            return existing;
        }

        var builder = environment.ApplicationBuilder;

        var minio = builder.AddContainer($"{environment.Resource.Name}-r2-minio", Image, Tag)
            .WithImageRegistry("ghcr.io")
            .WithImageSHA256(ImageDigest)
            .WithContainerName($"{environment.Resource.Name}-r2-minio")
            .WithArgs("server", "/data", "--console-address", $":{ConsolePort}")
            .WithEnvironment("MINIO_ROOT_USER", DefaultAccessKey)
            .WithEnvironment("MINIO_ROOT_PASSWORD", DefaultSecretKey)
            .WithHttpEndpoint(targetPort: ApiPort, name: S3EndpointName)
            .WithHttpEndpoint(targetPort: ConsolePort, name: "console")
            .WithVolume($"{environment.Resource.Name}-r2-minio-data", "/data")
            .WithHttpHealthCheck("/minio/health/live", endpointName: S3EndpointName);

        var endpoint = minio.GetEndpoint(S3EndpointName);
        var annotation = new MinioEmulatorAnnotation(minio, endpoint, DefaultAccessKey, DefaultSecretKey);
        annotation.Buckets.Add(bucket);
        environment.Resource.Annotations.Add(annotation);

        // Bootstrap: create the buckets in MinIO once it is healthy/ready.
        builder.Eventing.Subscribe<ResourceReadyEvent>(minio.Resource, (evt, ct) =>
            CreateBucketsAsync(annotation, evt.Services, ct));

        return annotation;
    }

    private static async Task CreateBucketsAsync(
        MinioEmulatorAnnotation annotation,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var logger = services.GetService<ILoggerFactory>()?.CreateLogger("Cloudflare.R2.MinioEmulator");
        var notifications = services.GetRequiredService<ResourceNotificationService>();
        var serviceUrl = annotation.S3Endpoint.Url;

        var config = new AmazonS3Config
        {
            ServiceURL = serviceUrl,
            ForcePathStyle = true,
            AuthenticationRegion = "auto",
        };
        using var s3 = new AmazonS3Client(new BasicAWSCredentials(annotation.AccessKey, annotation.SecretKey), config);

        foreach (var bucket in annotation.Buckets.Distinct())
        {
            await CreateBucketWithRetryAsync(s3, bucket.BucketName, logger, cancellationToken).ConfigureAwait(false);
            await notifications.PublishUpdateAsync(bucket, snapshot => snapshot with
            {
                State = KnownResourceStates.Running,
            }).ConfigureAwait(false);
        }
    }

    private static async Task CreateBucketWithRetryAsync(
        IAmazonS3 s3,
        string bucket,
        ILogger? logger,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 12;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await s3.PutBucketAsync(new PutBucketRequest { BucketName = bucket }, cancellationToken).ConfigureAwait(false);
                logger?.LogInformation("Created R2 emulator bucket '{Bucket}'.", bucket);
                return;
            }
            catch (AmazonS3Exception ex) when (
                ex.ErrorCode is "BucketAlreadyOwnedByYou" or "BucketAlreadyExists")
            {
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && ex is HttpRequestException or AmazonS3Exception or AmazonServiceException)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), cancellationToken).ConfigureAwait(false);
            }
        }

        logger?.LogWarning("Gave up creating R2 emulator bucket '{Bucket}' after {Attempts} attempts.", bucket, maxAttempts);
    }
}
