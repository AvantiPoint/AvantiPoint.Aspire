using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Tests;

public class CloudflareWorkerTests
{
    [Fact]
    public void AddCloudflareWorker_Adds_Worker_Resource_For_Environment()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        var cf = builder.AddCloudflareEnvironment();

        var worker = cf.AddCloudflareWorker("worker", AppContext.BaseDirectory);

        var resource = Assert.Single(builder.Resources.OfType<CloudflareWorkerResource>());
        Assert.Same(cf.Resource, resource.Environment);
        Assert.Same(worker.Resource, resource);
    }

    [Fact]
    public void AddCloudflareWorker_RunMode_Exposes_Http_Endpoint()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        var cf = builder.AddCloudflareEnvironment();

        var worker = cf.AddCloudflareWorker("worker", AppContext.BaseDirectory, o => o.Port = 8790);

        var endpoint = Assert.Single(worker.Resource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal("http", endpoint.Name);
        Assert.Equal(8790, endpoint.Port);
    }

    [Fact]
    public void AddCloudflareWorker_Requires_Workers_Scope_And_Registers_Target()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        var cf = builder.AddCloudflareEnvironment();

        cf.AddCloudflareWorker("worker", AppContext.BaseDirectory);

        Assert.Contains(CloudflareScopes.WorkersScriptsEdit, cf.Resource.RequiredScopes);
        Assert.Contains(builder.Services, d =>
            d.ServiceType == typeof(ICloudflarePublishTarget) &&
            d.ImplementationType == typeof(WorkerPublishTarget));
    }

    [Fact]
    public void WorkerPublishTarget_Only_Handles_Workers()
    {
        var target = new WorkerPublishTarget(NullLogger<WorkerPublishTarget>.Instance, new StubWrangler());

        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        var cf = builder.AddCloudflareEnvironment();
        var worker = cf.AddCloudflareWorker("worker", AppContext.BaseDirectory).Resource;
        var container = builder.AddContainer("other", "nginx").Resource;

        Assert.True(target.CanHandle(worker));
        Assert.False(target.CanHandle(container));
    }

    private sealed class StubWrangler : AvantiPoint.Aspire.Hosting.Cloudflare.Cli.IWranglerCli
    {
        public Task<string?> GetVersionAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>("test");
        public Task<AvantiPoint.Aspire.Hosting.Cloudflare.Cli.CliResult> RunAsync(
            IReadOnlyList<string> arguments, string? workingDirectory = null, string? apiToken = null,
            string? accountId = null, Microsoft.Extensions.Logging.ILogger? logger = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new AvantiPoint.Aspire.Hosting.Cloudflare.Cli.CliResult { ExitCode = 0, StandardOutput = "", StandardError = "" });
    }
}
