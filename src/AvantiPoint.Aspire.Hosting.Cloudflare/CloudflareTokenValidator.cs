using AvantiPoint.Aspire.Hosting.Cloudflare.Api;
using Microsoft.Extensions.Logging;

namespace AvantiPoint.Aspire.Hosting.Cloudflare;

/// <summary>
/// Validates a Cloudflare API token up front so provisioning/deploy fails fast with a clear
/// message instead of midway through. Validation is cached per token for the lifetime of the
/// process so it runs once even when multiple resources trigger it.
/// </summary>
internal sealed class CloudflareTokenValidator(ICloudflareApiClient apiClient, ILogger<CloudflareTokenValidator> logger)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _validatedTokenHash;

    /// <summary>
    /// Ensures the supplied token is active. <paramref name="requiredScopes"/> is the set of
    /// human-readable permission scopes the operation needs; they are surfaced in error/log
    /// output to help the user mint a correctly-scoped token.
    /// </summary>
    public async Task ValidateAsync(string apiToken, IReadOnlyCollection<string> requiredScopes, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiToken))
        {
            throw new CloudflareApiException(
                "No Cloudflare API token was provided. Set CLOUDFLARE_API_TOKEN (env var or user-secrets) " +
                "or pass a parameter to AddCloudflareEnvironment.");
        }

        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(apiToken)));

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_validatedTokenHash == hash)
            {
                return;
            }

            // Verification checks activeness, not permissions. Service APIs enforce the required scopes.
            var result = await apiClient.VerifyTokenAsync(apiToken, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(result.Status, "active", StringComparison.OrdinalIgnoreCase))
            {
                throw new CloudflareApiException(
                    $"Cloudflare API token is not active (status: '{result.Status}'). " +
                    $"Mint a new token with these scopes: {FormatScopes(requiredScopes)}.");
            }

            logger.LogInformation(
                "Cloudflare API token verified (id: {TokenId}). Operation requires scopes: {Scopes}.",
                result.Id, FormatScopes(requiredScopes));

            _validatedTokenHash = hash;
        }
        finally
        {
            _gate.Release();
        }

    }

    private static string FormatScopes(IReadOnlyCollection<string> scopes)
        => scopes.Count == 0 ? "(none)" : string.Join(", ", scopes.Distinct(StringComparer.OrdinalIgnoreCase));
}
