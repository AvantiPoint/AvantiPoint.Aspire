using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare.Cli;
using AvantiPoint.Aspire.Hosting.Cloudflare.Pages.Publishing;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Pages.Tests;

public class PagesBuildEnvironmentTests
{
    [Fact]
    public async Task Build_process_receives_publish_values_and_production_endpoint()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"aspire-pages-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(directory, "build.cjs"),
                "require('node:fs').writeFileSync('result.txt', [process.env.VITE_API_URL, process.env.BUILD_MODE, process.env.BUILD_TOKEN].join('\\n'));", TestContext.Current.CancellationToken);
            var builder = DistributedApplication.CreateBuilder([]);
            var environment = builder.AddCloudflareEnvironment();
            var api = builder.AddResource(new ProjectResource("api"))
                .WithHttpEndpoint(name: "http")
                .PublishAsCloudflareContainer(environment)
                .WithCustomDomain("api.example.com", "test-zone");
            var secret = builder.AddParameter("build-token", () => "test-only-secret", secret: true);
            var web = builder.AddViteApp("web", directory)
                .WithEnvironment("VITE_API_URL", ReferenceExpression.Create($"{api.GetEndpoint("http")}/v1"))
                .WithEnvironment("BUILD_TOKEN", secret)
                .WithEnvironment(context => context.EnvironmentVariables["BUILD_MODE"] = context.ExecutionContext.IsPublishMode ? "publish" : "run")
                .PublishAsCloudflarePages(environment, options => options.BuildCommand = "node build.cjs");
            var logger = new RecordingLogger<PagesPublishTarget>();
            var target = new PagesPublishTarget(logger, Substitute.For<IWranglerCli>());
            await target.GenerateArtifactsAsync(new CloudflarePublishContext { Step = null!, Environment = environment.Resource }, web.Resource, TestContext.Current.CancellationToken);
            Assert.Equal(["https://api.example.com/v1", "publish", "test-only-secret"], await File.ReadAllLinesAsync(Path.Combine(directory, "result.txt"), TestContext.Current.CancellationToken));
            Assert.DoesNotContain(logger.Messages, message => message.Contains("test-only-secret", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Endpoint_without_production_hostname_fails_before_build()
    {
        var builder = DistributedApplication.CreateBuilder([]);
        var environment = builder.AddCloudflareEnvironment();
        var api = builder.AddResource(new ProjectResource("api")).WithHttpEndpoint(name: "http").PublishAsCloudflareContainer(environment);
        var web = builder.AddViteApp("web", AppContext.BaseDirectory).WithEnvironment("VITE_API_URL", api.GetEndpoint("http"));
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => PagesBuildEnvironment.ResolveAsync(web.Resource, NullLogger.Instance, TestContext.Current.CancellationToken));
        Assert.Contains("requires a Cloudflare target with a custom domain", exception.Message);
    }
}
