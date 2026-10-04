using System.Diagnostics;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.IntegrationTests;

internal static class AspirePublishingChecks
{
    public const string AppHostVariable = "CLOUDFLARE_TYPESCRIPT_APPHOST";

    public static async Task RunAsync(string command, string name, string directory, CancellationToken cancellationToken)
    {
        var appHost = Environment.GetEnvironmentVariable(AppHostVariable)
            ?? throw new InvalidOperationException("The TypeScript integration AppHost was not configured.");
        var startInfo = new ProcessStartInfo("aspire")
        {
            WorkingDirectory = Path.GetDirectoryName(appHost)!,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in new[] { command, "--apphost", appHost, "--output-path", directory, "--non-interactive" })
            startInfo.ArgumentList.Add(argument);
        startInfo.Environment["CLOUDFLARE_PUBLISH_TEST"] = "1";
        startInfo.Environment["CLOUDFLARE_PUBLISH_TEST_NAME"] = name;
        startInfo.Environment["CLOUDFLARE_PUBLISH_TEST_DIRECTORY"] = directory;
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start Aspire for TypeScript publishing.");
        var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = process.StandardError.ReadToEndAsync(cancellationToken);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(4));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            cancellationToken.ThrowIfCancellationRequested();
            throw new TimeoutException($"Aspire TypeScript {command} exceeded four minutes.");
        }
        var diagnostic = $"{await output}\n{await error}";
        foreach (var value in new[] { CloudflareAccount.Token, CloudflareAccount.AccountId })
            if (!string.IsNullOrEmpty(value)) diagnostic = diagnostic.Replace(value, "***", StringComparison.Ordinal);
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Aspire TypeScript {command} failed (exit {process.ExitCode}): {diagnostic}");
    }
}
