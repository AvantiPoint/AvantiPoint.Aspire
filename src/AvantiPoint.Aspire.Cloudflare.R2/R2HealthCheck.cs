using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AvantiPoint.Aspire.Cloudflare.R2;

/// <summary>
/// Health check that verifies connectivity to R2 (or the MinIO emulator). When a bucket is known it
/// checks the bucket; otherwise it lists buckets.
/// </summary>
internal sealed class R2HealthCheck(IAmazonS3 client, string? bucketName) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!string.IsNullOrEmpty(bucketName))
            {
                await client.GetBucketLocationAsync(new GetBucketLocationRequest { BucketName = bucketName }, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await client.ListBucketsAsync(new ListBucketsRequest(), cancellationToken).ConfigureAwait(false);
            }

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("R2 connectivity check failed.", ex);
        }
    }
}
