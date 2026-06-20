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
}
