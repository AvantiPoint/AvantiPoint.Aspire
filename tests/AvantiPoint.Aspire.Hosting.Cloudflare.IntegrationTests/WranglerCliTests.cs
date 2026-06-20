using AvantiPoint.Aspire.Hosting.Cloudflare.Cli;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.IntegrationTests;

/// <summary>
/// Verifies the wrangler process plumbing (including the Windows <c>cmd /c</c> wrapping in CliRunner).
/// Skipped when wrangler is not installed (e.g. on a CI runner without npm tooling).
/// </summary>
public class WranglerCliTests
{
    [Fact]
    public async Task GetVersion_Returns_Version_When_Wrangler_Installed()
    {
        var cli = new WranglerCli(NullLogger<WranglerCli>.Instance);

        var version = await cli.GetVersionAsync(TestContext.Current.CancellationToken);

        Assert.SkipWhen(version is null, "wrangler is not installed on this machine.");
        Assert.Matches(@"\d+\.\d+", version!);
    }
}
