using Microsoft.Extensions.Logging;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Cli;

/// <summary>Wraps the Cloudflare <c>wrangler</c> CLI used to deploy Workers/Containers/Pages.</summary>
public interface IWranglerCli
{
    /// <summary>Returns the installed wrangler version, or <c>null</c> if wrangler is not available.</summary>
    Task<string?> GetVersionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a wrangler command. <paramref name="apiToken"/>/<paramref name="accountId"/> are passed as the
    /// <c>CLOUDFLARE_API_TOKEN</c>/<c>CLOUDFLARE_ACCOUNT_ID</c> environment variables for non-interactive auth.
    /// </summary>
    Task<CliResult> RunAsync(
        IReadOnlyList<string> arguments,
        string? workingDirectory = null,
        string? apiToken = null,
        string? accountId = null,
        ILogger? logger = null,
        CancellationToken cancellationToken = default);
}
