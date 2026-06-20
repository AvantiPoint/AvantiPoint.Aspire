using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.IntegrationTests;

/// <summary>
/// Helpers for gating integration tests that need a real Cloudflare account.
/// Tests are skipped (not failed) when credentials are absent, so local/CI runs
/// without the secret stay green.
/// </summary>
internal static class CloudflareAccount
{
    public const string TokenVar = "CLOUDFLARE_API_TOKEN";
    public const string AccountVar = "CLOUDFLARE_ACCOUNT_ID";

    public static string? Token => Environment.GetEnvironmentVariable(TokenVar);
    public static string? AccountId => Environment.GetEnvironmentVariable(AccountVar);

    public static bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Token) && !string.IsNullOrWhiteSpace(AccountId);
}

public class IntegrationGateTests
{
    [Fact]
    public void Gate_Skips_Without_Credentials()
    {
        Assert.SkipUnless(CloudflareAccount.IsConfigured,
            $"Set {CloudflareAccount.TokenVar} and {CloudflareAccount.AccountVar} to run integration tests.");

        // Real integration assertions land here as features are implemented.
        Assert.False(string.IsNullOrEmpty(CloudflareAccount.Token));
    }
}
