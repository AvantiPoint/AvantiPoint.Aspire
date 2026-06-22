using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Tests;

public class EnvironmentResolutionTests
{
    [Fact]
    public void PublishAsCloudflareContainer_NoArg_Resolves_Single_Environment()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        var cf = builder.AddCloudflareEnvironment();

        var api = builder.AddResource(new ProjectResource("api")).PublishAsCloudflareContainer();

        var annotation = Assert.Single(api.Resource.Annotations.OfType<ICloudflareTargetAnnotation>());
        Assert.Same(cf.Resource, annotation.Environment);
    }

    [Fact]
    public void AddCloudflareWorker_NoEnv_Resolves_From_Builder()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        var cf = builder.AddCloudflareEnvironment();

        var worker = builder.AddCloudflareWorker("worker", AppContext.BaseDirectory);

        Assert.Same(cf.Resource, worker.Resource.Environment);
    }

    [Fact]
    public void Resolution_Throws_When_No_Environment()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());

        Assert.Throws<InvalidOperationException>(
            () => builder.AddResource(new ProjectResource("api")).PublishAsCloudflareContainer());
    }

    [Fact]
    public void Resolution_Throws_When_Multiple_Environments()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        builder.AddCloudflareEnvironment("one");
        builder.AddCloudflareEnvironment("two");

        Assert.Throws<InvalidOperationException>(
            () => builder.AddResource(new ProjectResource("api")).PublishAsCloudflareContainer());
    }
}
