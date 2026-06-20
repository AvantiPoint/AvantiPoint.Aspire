using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.R2;

/// <summary>
/// An R2 bucket modeled as an Aspire resource. Exposes an S3-compatible connection string that
/// points at the local MinIO emulator during <c>aspire run</c> (the default) or at the real R2
/// account when <see cref="R2HostingExtensions.RunAsReal"/> is used / during deploy.
/// </summary>
public sealed class R2BucketResource : Resource, IResourceWithConnectionString, IResourceWithWaitSupport, ICloudflareResource
{
    internal R2BucketResource(string name, CloudflareEnvironmentResource environment, string bucketName)
        : base(name)
    {
        Environment = environment;
        BucketName = bucketName;
    }

    /// <summary>The Cloudflare environment that owns this bucket.</summary>
    public CloudflareEnvironmentResource Environment { get; }

    /// <summary>The actual R2 bucket name (may differ from the Aspire resource name).</summary>
    public string BucketName { get; }

    /// <summary>Optional R2 location hint (e.g. <c>weur</c>).</summary>
    public string? LocationHint { get; internal set; }

    /// <summary>True when this bucket is served by the local MinIO emulator (run mode default).</summary>
    public bool UseEmulator { get; internal set; }

    /// <summary>When true, <c>aspire deploy --destroy</c> will delete the real R2 bucket. Off by default (data-loss guard).</summary>
    public bool AllowDestroy { get; internal set; }

    // Emulator wiring (set when UseEmulator is true).
    internal EndpointReference? EmulatorEndpoint { get; set; }
    internal string? EmulatorAccessKey { get; set; }
    internal string? EmulatorSecretKey { get; set; }

    // Real R2 wiring (set when UseEmulator is false).
    internal ParameterResource? RealAccessKey { get; set; }
    internal ParameterResource? RealSecretKey { get; set; }

    /// <inheritdoc />
    public ReferenceExpression ConnectionStringExpression => BuildConnectionString();

    private ReferenceExpression BuildConnectionString()
    {
        if (UseEmulator)
        {
            var endpoint = EmulatorEndpoint
                ?? throw new InvalidOperationException($"R2 emulator endpoint for bucket '{Name}' has not been wired.");

            return ReferenceExpression.Create(
                $"Endpoint=http://{endpoint.Property(EndpointProperty.Host)}:{endpoint.Property(EndpointProperty.Port)};AccessKey={EmulatorAccessKey};SecretKey={EmulatorSecretKey};Bucket={BucketName};Region=auto");
        }

        var accountId = Environment.AccountId;
        var accessKey = RealAccessKey
            ?? throw new InvalidOperationException($"R2 access key for bucket '{Name}' has not been configured.");
        var secretKey = RealSecretKey
            ?? throw new InvalidOperationException($"R2 secret key for bucket '{Name}' has not been configured.");

        return ReferenceExpression.Create(
            $"Endpoint=https://{accountId}.r2.cloudflarestorage.com;AccessKey={accessKey};SecretKey={secretKey};Bucket={BucketName};Region=auto");
    }
}
