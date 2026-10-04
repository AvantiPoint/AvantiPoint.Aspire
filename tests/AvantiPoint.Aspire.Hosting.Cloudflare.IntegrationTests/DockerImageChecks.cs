using System.Diagnostics;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.IntegrationTests;

internal static class DockerImageChecks
{
    public static async Task PullAsync(string image, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("docker")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("pull");
        startInfo.ArgumentList.Add(image);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start Docker to pull the emulator image.");
        var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = process.StandardError.ReadToEndAsync(cancellationToken);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
            cancellationToken.ThrowIfCancellationRequested();
            throw new TimeoutException($"Docker did not finish pulling emulator image '{image}' within two minutes.");
        }

        await output;
        var diagnostic = await error;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Docker could not pull emulator image '{image}': {diagnostic}");
        }
    }
}
