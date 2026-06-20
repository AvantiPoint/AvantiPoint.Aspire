using AvantiPoint.Aspire.Hosting.Cloudflare.Api.Models;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Api;

/// <summary>Raised when a Cloudflare API call fails or returns <c>success: false</c>.</summary>
public sealed class CloudflareApiException : Exception
{
    public CloudflareApiException(string message)
        : base(message)
    {
    }

    public CloudflareApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public IReadOnlyList<CloudflareApiError> Errors { get; init; } = [];

    internal static CloudflareApiException FromResponse(string operation, IReadOnlyList<CloudflareApiError> errors)
    {
        var detail = errors.Count > 0
            ? string.Join("; ", errors.Select(e => e.ToString()))
            : "no error detail returned";
        return new CloudflareApiException($"Cloudflare API call '{operation}' failed: {detail}")
        {
            Errors = errors,
        };
    }
}
