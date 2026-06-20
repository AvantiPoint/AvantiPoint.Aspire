using Microsoft.Extensions.Logging;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Cli;

/// <summary>Default <see cref="IWranglerCli"/> that shells out to the <c>wrangler</c> executable on PATH.</summary>
internal sealed class WranglerCli(ILogger<WranglerCli> logger) : IWranglerCli
{
    private const string Executable = "wrangler";

    private readonly ILogger<WranglerCli> _logger = logger;

    public async Task<string?> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await CliRunner.RunAsync(Executable, ["--version"], cancellationToken: cancellationToken).ConfigureAwait(false);
            return result.Success ? result.StandardOutput.Trim() : null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public async Task<CliResult> RunAsync(
        IReadOnlyList<string> arguments,
        string? workingDirectory = null,
        string? apiToken = null,
        string? accountId = null,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        var environment = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (!string.IsNullOrEmpty(apiToken))
        {
            environment["CLOUDFLARE_API_TOKEN"] = apiToken;
        }

        if (!string.IsNullOrEmpty(accountId))
        {
            environment["CLOUDFLARE_ACCOUNT_ID"] = accountId;
        }

        var result = await CliRunner.RunAsync(
            Executable, arguments, workingDirectory, environment, logger ?? _logger, cancellationToken).ConfigureAwait(false);

        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"wrangler {string.Join(' ', arguments)} failed (exit {result.ExitCode}). " +
                $"Ensure wrangler is installed (npm i -g wrangler) and the API token is valid.\n{result.StandardError}");
        }

        return result;
    }
}
