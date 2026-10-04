using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using System.Text;
using AvantiPoint.Aspire.Hosting.Cloudflare.Cli;
using AvantiPoint.Aspire.Hosting.Cloudflare.Pages.Publishing;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
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
                "require('node:fs').writeFileSync('result.txt', [process.env.VITE_API_URL, process.env.BUILD_MODE, process.env.BUILD_TOKEN, process.env.BUILD_JSON, process.env.BUILD_ENCODED_JSON, process.env.BUILD_SERVICE, process.env.BUILD_PROVIDER].join('\\n'));", TestContext.Current.CancellationToken);
            var builder = DistributedApplication.CreateBuilder([]);
            var environment = builder.AddCloudflareEnvironment();
            var api = builder.AddResource(new ProjectResource("api"))
                .WithHttpEndpoint(name: "http")
                .PublishAsCloudflareContainer(environment)
                .WithCustomDomain("api.example.com", "test-zone");
            var secret = builder.AddParameter("build-token", () => "test-only-secret", secret: true);
            var json = builder.AddParameter("build-json", () => "{\"key\":\"value\"}");
            using var services = new ServiceCollection().AddSingleton(new StringBuilder("from-services")).BuildServiceProvider();
            var web = builder.AddViteApp("web", directory)
                .WithEnvironment("VITE_API_URL", ReferenceExpression.Create($"{api.GetEndpoint("http")}/v1"))
                .WithEnvironment("BUILD_TOKEN", secret)
                .WithEnvironment("BUILD_JSON", ReferenceExpression.Create($"prefix:{json}"))
                .WithEnvironment("BUILD_ENCODED_JSON", ReferenceExpression.Create($"{json:uri}"))
                .WithEnvironment(context => context.EnvironmentVariables["BUILD_MODE"] = context.ExecutionContext.IsPublishMode ? "publish" : "run")
                .WithEnvironment(context => context.EnvironmentVariables["BUILD_SERVICE"] = context.ExecutionContext.Services.GetRequiredService<StringBuilder>().ToString())
                .WithEnvironment(context => context.EnvironmentVariables["BUILD_PROVIDER"] = new ServiceBuildValue())
                .PublishAsCloudflarePages(environment, options => options.BuildCommand = "node build.cjs");
            var logger = new RecordingLogger<PagesPublishTarget>();
            var target = new PagesPublishTarget(logger, Substitute.For<IWranglerCli>());
            var context = CreateContext(environment.Resource, services);
            await target.GenerateArtifactsAsync(context, web.Resource, TestContext.Current.CancellationToken);
            Assert.Equal(["https://api.example.com/v1", "publish", "test-only-secret", "prefix:{\"key\":\"value\"}", "%7B%22key%22%3A%22value%22%7D", "from-services", "from-services"], await File.ReadAllLinesAsync(Path.Combine(directory, "result.txt"), TestContext.Current.CancellationToken));
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
        using var services = new ServiceCollection().BuildServiceProvider();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => PagesBuildEnvironment.ResolveAsync(web.Resource, services, NullLogger.Instance, TestContext.Current.CancellationToken));
        Assert.Contains("requires a Cloudflare target with a custom domain", exception.Message);
    }

    private static CloudflarePublishContext CreateContext(CloudflareEnvironmentResource environment, IServiceProvider services)
    {
#pragma warning disable ASPIREPIPELINES001 // Exercise the same services-bearing context used by the publish pipeline.
        var pipeline = new PipelineContext(new DistributedApplicationModel([]), new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish), services, NullLogger.Instance, TestContext.Current.CancellationToken);
        return new CloudflarePublishContext
        {
            Environment = environment,
            Step = new PipelineStepContext { PipelineContext = pipeline, ReportingStep = null! },
        };
#pragma warning restore ASPIREPIPELINES001
    }
}
