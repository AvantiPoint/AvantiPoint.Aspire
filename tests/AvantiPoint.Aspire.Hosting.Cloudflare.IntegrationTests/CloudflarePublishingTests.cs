using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api;
using AvantiPoint.Aspire.Hosting.Cloudflare.Pipeline;
using AvantiPoint.Aspire.Hosting.Cloudflare.R2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.IntegrationTests;

public class CloudflarePublishingTests
{
    [Theory(Timeout = 600_000)]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Publish_Deploy_Worker_With_Real_R2_Binding_And_Verified_Cleanup(bool typeScript)
    {
        Assert.SkipUnless(CloudflareAccount.IsConfigured, "Cloudflare integration credentials are required.");
        Assert.SkipUnless(!typeScript || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(AspirePublishingChecks.AppHostVariable)),
            "The TypeScript AppHost, generated SDK and Aspire CLI are required.");
        var ct = TestContext.Current.CancellationToken;
        var name = $"ap-aspire-it-{Guid.NewGuid():N}"[..40];
        var directory = Path.Combine(Path.GetTempPath(), name);
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, "wrangler.jsonc"), JsonSerializer.Serialize(new
        {
            name,
            main = "worker.js",
            compatibility_date = "2026-10-01",
            workers_dev = true,
            r2_buckets = new[] { new { binding = "BUCKET", bucket_name = name } },
        }), ct);
        await File.WriteAllTextAsync(Path.Combine(directory, "worker.js"), """
            export default {
              async fetch(request, env) {
                if (request.method === 'DELETE') {
                  await env.BUCKET.delete('probe');
                  return new Response('cleaned');
                }
                if (request.method === 'POST') await env.BUCKET.put('probe', 'AvantiPoint Aspire publishing');
                const object = await env.BUCKET.get('probe');
                return new Response(object ? await object.text() : 'missing', { status: object ? 200 : 404 });
              }
            };
            """, ct);

        var builder = DistributedApplication.CreateBuilder(["--operation", "publish", "--output-path", directory]);
        var environment = builder.AddCloudflareEnvironment();
        var bucket = environment.AddR2Bucket("uploads", bucketName: name).RunAsEmulator().AllowDeletion();
        builder.AddCloudflareWorker(name, directory);
        Assert.False(bucket.Resource.UseEmulator);
        Assert.Empty(builder.Resources.OfType<ContainerResource>());
        await using var app = builder.Build();
        var api = app.Services.GetRequiredService<ICloudflareApiClient>();
        using var accountHttp = new HttpClient { BaseAddress = CloudflareApiClient.BaseAddress };
        accountHttp.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CloudflareAccount.Token);
        using var subdomainResponse = await accountHttp.GetAsync($"accounts/{CloudflareAccount.AccountId}/workers/subdomain", ct);
        Assert.True(subdomainResponse.IsSuccessStatusCode, $"Could not read Workers subdomain (HTTP {(int)subdomainResponse.StatusCode}).");
        using var subdomainJson = JsonDocument.Parse(await subdomainResponse.Content.ReadAsStringAsync(ct));
        var subdomain = subdomainJson.RootElement.GetProperty("result").GetProperty("subdomain").GetString();
        using var workerHttp = new HttpClient
        {
            BaseAddress = new Uri($"https://{name}.{subdomain}.workers.dev/"),
            Timeout = TimeSpan.FromSeconds(15),
        };

        var pipeline = new PipelineContext(new DistributedApplicationModel(builder.Resources), builder.ExecutionContext,
            app.Services, NullLogger.Instance, ct);
        var context = new PipelineStepContext { PipelineContext = pipeline, ReportingStep = null! };
        var steps = CloudflarePipelineSteps.CreateSteps(environment.Resource).ToDictionary(step => step.Name);
        var deployed = false;
        var probeMayExist = false;
        try
        {
            await steps[CloudflarePipelineSteps.ValidateTokenStepName(environment.Resource)].Action(context);
            await steps[CloudflarePipelineSteps.PublishStepName(environment.Resource)].Action(context);
            // Enter cleanup even if deployment only completes its first resource.
            deployed = true;
            if (typeScript)
                await AspirePublishingChecks.RunAsync("deploy", name, directory, ct);
            else
                await steps[CloudflarePipelineSteps.DeployStepName(environment.Resource)].Action(context);
            Assert.NotNull(await api.GetR2BucketAsync(CloudflareAccount.Token!, CloudflareAccount.AccountId!, name, ct));

            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    // A timed-out POST can still have written the object on the server.
                    probeMayExist = true;
                    using var response = await workerHttp.PostAsync("", new StringContent(""), ct);
                    response.EnsureSuccessStatusCode();
                    Assert.Equal("AvantiPoint Aspire publishing", await response.Content.ReadAsStringAsync(ct));
                    Assert.Equal("AvantiPoint Aspire publishing", await workerHttp.GetStringAsync("", ct));
                    break;
                }
                catch (Exception error) when ((error is HttpRequestException or TaskCanceledException) && attempt < 20 && !ct.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), ct);
                }
            }
            Console.WriteLine($"{(typeScript ? "TypeScript" : "C#")} real Cloudflare publish/deploy, Worker HTTP content and R2 binding verified.");
        }
        finally
        {
            try
            {
                if (deployed)
                {
                    using var fallbackTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                    var fallbackCt = fallbackTimeout.Token;
                    // Keep the object-removal Worker until the bucket is confirmed empty.
                    // If this fails, retain it for recovery rather than stranding a nonempty bucket.
                    if (probeMayExist)
                        await EmptyProbeAsync(workerHttp, name, fallbackCt);
                    // A partly failed deploy must not strand the earlier R2 resource.
                    // Attempt both removals independently even if Worker HTTP/DNS never became ready.
                    try
                    {
                        using var response = await accountHttp.DeleteAsync($"accounts/{CloudflareAccount.AccountId}/workers/scripts/{name}", fallbackCt);
                        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound,
                            $"Could not remove integration Worker (HTTP {(int)response.StatusCode}).");
                        using var absentWorker = await accountHttp.GetAsync($"accounts/{CloudflareAccount.AccountId}/workers/scripts/{name}", fallbackCt);
                        Assert.Equal(HttpStatusCode.NotFound, absentWorker.StatusCode);
                    }
                    finally
                    {
                        using var bucketTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(1));
                        await api.DeleteR2BucketAsync(CloudflareAccount.Token!, CloudflareAccount.AccountId!, name, bucketTimeout.Token);
                        Assert.Null(await api.GetR2BucketAsync(CloudflareAccount.Token!, CloudflareAccount.AccountId!, name, bucketTimeout.Token));
                        Console.WriteLine($"Cleanup verified for integration resource '{name}'.");
                    }
                }
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private static async Task EmptyProbeAsync(HttpClient worker, string name, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                using var response = await worker.DeleteAsync("", cancellationToken);
                response.EnsureSuccessStatusCode();
                return;
            }
            catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
            {
                if (attempt == 4 || cancellationToken.IsCancellationRequested)
                    throw new InvalidOperationException($"Could not empty test bucket '{name}'. Its cleanup Worker is retained for recovery.", error);
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }
    }
}
