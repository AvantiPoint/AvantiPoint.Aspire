using AvantiPoint.Aspire.Hosting.Cloudflare.Api;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.IntegrationTests;

/// <summary>
/// Exercises the real R2 deploy path (token verify + bucket create/get/delete) — the same calls
/// <c>R2PublishTarget.DeployAsync</c> makes during <c>aspire deploy</c>. Skipped without credentials.
/// </summary>
public class R2DeployIntegrationTests
{
    private static CloudflareApiClient CreateClient()
    {
        var http = new HttpClient { BaseAddress = CloudflareApiClient.BaseAddress };
        return new CloudflareApiClient(http, NullLogger<CloudflareApiClient>.Instance);
    }

    [Fact]
    public async Task Verify_Token_Succeeds_With_Real_Credentials()
    {
        Assert.SkipUnless(CloudflareAccount.IsConfigured,
            $"Set {CloudflareAccount.TokenVar}/{CloudflareAccount.AccountVar} to run.");

        var client = CreateClient();
        var result = await client.VerifyTokenAsync(CloudflareAccount.Token!, TestContext.Current.CancellationToken);

        Assert.Equal("active", result.Status, ignoreCase: true);
    }

    [Fact]
    public async Task Create_Get_Delete_R2_Bucket_RoundTrips()
    {
        Assert.SkipUnless(CloudflareAccount.IsConfigured,
            $"Set {CloudflareAccount.TokenVar}/{CloudflareAccount.AccountVar} to run.");

        var client = CreateClient();
        var token = CloudflareAccount.Token!;
        var account = CloudflareAccount.AccountId!;
        var ct = TestContext.Current.CancellationToken;
        var bucketName = $"ap-aspire-it-{Guid.NewGuid():N}"[..40].ToLowerInvariant();

        try
        {
            var created = await client.CreateR2BucketAsync(token, account, new CreateR2BucketRequest { Name = bucketName }, ct);
            Assert.Equal(bucketName, created.Name);

            // Idempotent: a second create returns the existing bucket rather than throwing.
            var again = await client.CreateR2BucketAsync(token, account, new CreateR2BucketRequest { Name = bucketName }, ct);
            Assert.Equal(bucketName, again.Name);

            var fetched = await client.GetR2BucketAsync(token, account, bucketName, ct);
            Assert.NotNull(fetched);
        }
        finally
        {
            await client.DeleteR2BucketAsync(token, account, bucketName, ct);
        }
    }
}
