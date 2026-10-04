using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.IntegrationTests;

/// <summary>
/// Full local end-to-end of the playground: the seeder uploads data.json to the R2 (MinIO) bucket,
/// the API reads it from R2 and serves it at <c>/data</c>. Requires Docker for the MinIO emulator.
/// </summary>
public class PlaygroundEndToEndTests
{
    [Fact(Timeout = 600_000)] // hard cap so a stuck orchestration can never hang CI
    public async Task Seeder_Populates_R2_And_Api_Serves_The_Data()
    {
        Assert.SkipUnless(DockerIsAvailable(), "Docker is required for the MinIO R2 emulator.");

        var ct = TestContext.Current.CancellationToken;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CloudflarePlayground_AppHost>(ct);
        await using var app = await appHost.BuildAsync(ct);

        // Diagnose unavailable emulator images before Aspire starts dependent resources.
        foreach (var container in appHost.Resources.OfType<ContainerResource>())
        {
            Assert.True(container.TryGetContainerImageName(out var image));
            await DockerImageChecks.PullAsync(image, ct);
        }

        await app.StartAsync(ct).WaitAsync(TimeSpan.FromMinutes(2), ct);

        var timeout = TimeSpan.FromMinutes(5);

        // The seeder runs to completion (uploads data.json), then the API starts (it waits on the seeder).
        await app.ResourceNotifications
            .WaitForResourceAsync("seeder", KnownResourceStates.Finished, ct)
            .WaitAsync(timeout, ct);

        await app.ResourceNotifications
            .WaitForResourceAsync("api", KnownResourceStates.Running, ct)
            .WaitAsync(timeout, ct);

        using var http = app.CreateHttpClient("api", "http");

        // Retry briefly while the API finishes binding.
        JsonElement data = default;
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                data = await http.GetFromJsonAsync<JsonElement>("/data", ct);
                break;
            }
            catch when (attempt < 15)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }
        }

        Assert.Equal("AvantiPoint Aspire for Cloudflare", data.GetProperty("title").GetString());
        Assert.True(data.GetProperty("features").GetArrayLength() >= 1);
    }

    private static bool DockerIsAvailable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("docker", "info")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            });
            if (process is null)
            {
                return false;
            }

            process.WaitForExit(10_000);
            return process.HasExited && process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
