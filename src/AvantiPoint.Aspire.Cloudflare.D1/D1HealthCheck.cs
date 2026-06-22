using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AvantiPoint.Aspire.Cloudflare.D1;

/// <summary>Health check that verifies D1 connectivity with a trivial <c>SELECT 1</c>.</summary>
internal sealed class D1HealthCheck(ID1Client client) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await client.QueryFirstOrDefaultAsync<long>("SELECT 1", parameters: null, cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("D1 connectivity check failed.", ex);
        }
    }
}
