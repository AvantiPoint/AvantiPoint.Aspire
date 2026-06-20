using AvantiPoint.Aspire.Hosting.Cloudflare.Api.Models;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Api;

/// <summary>
/// Thin typed wrapper over the Cloudflare REST API used by the hosting integration for
/// provisioning and validation. There is no official Cloudflare .NET SDK, so this calls
/// the REST API directly. The API token is passed per call because it is resolved from an
/// Aspire parameter at provision/deploy time.
/// </summary>
public interface ICloudflareApiClient
{
    /// <summary>Calls <c>GET /user/tokens/verify</c>. Throws if the token is invalid/inactive.</summary>
    Task<TokenVerifyResult> VerifyTokenAsync(string apiToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an R2 bucket. Idempotent: if the bucket already exists it is returned as-is.
    /// </summary>
    Task<R2Bucket> CreateR2BucketAsync(string apiToken, string accountId, CreateR2BucketRequest request, CancellationToken cancellationToken = default);

    /// <summary>Gets an R2 bucket, or <c>null</c> if it does not exist.</summary>
    Task<R2Bucket?> GetR2BucketAsync(string apiToken, string accountId, string bucketName, CancellationToken cancellationToken = default);

    /// <summary>Deletes an R2 bucket. No-op if it does not exist.</summary>
    Task DeleteR2BucketAsync(string apiToken, string accountId, string bucketName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attaches a Worker to a custom domain (Workers custom domains). Cloudflare creates the DNS record
    /// and SSL certificate automatically. Idempotent.
    /// </summary>
    Task AttachWorkersCustomDomainAsync(string apiToken, string accountId, string zoneId, string hostname, string service, string environment = "production", CancellationToken cancellationToken = default);

    /// <summary>Adds a custom domain to a Pages project. Idempotent (existing domain is treated as success).</summary>
    Task AttachPagesDomainAsync(string apiToken, string accountId, string projectName, string hostname, CancellationToken cancellationToken = default);

    /// <summary>Creates or updates a proxied CNAME record in the zone (upsert by name).</summary>
    Task UpsertCnameRecordAsync(string apiToken, string zoneId, string name, string content, CancellationToken cancellationToken = default);
}
