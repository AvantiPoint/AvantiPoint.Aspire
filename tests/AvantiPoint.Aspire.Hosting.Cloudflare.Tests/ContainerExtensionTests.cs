using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Tests;

public class ContainerExtensionTests
{
    private static IResourceBuilder<ProjectResource> AddApi(IDistributedApplicationBuilder builder)
        => builder.AddResource(new ProjectResource("api"));

    [Fact]
    public void PublishAsCloudflareContainer_Adds_Container_Annotation()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        var cf = builder.AddCloudflareEnvironment();

        var api = AddApi(builder).PublishAsCloudflareContainer(cf);

        var annotation = Assert.Single(api.Resource.Annotations.OfType<ICloudflareTargetAnnotation>());
        Assert.Same(cf.Resource, annotation.Environment);
    }

    [Fact]
    public void PublishAsCloudflareContainer_Defaults_Names()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        var cf = builder.AddCloudflareEnvironment();

        var api = AddApi(builder).PublishAsCloudflareContainer(cf);

        var annotation = Assert.Single(api.Resource.Annotations.OfType<CloudflareContainerAnnotation>());
        Assert.Equal("api", annotation.WorkerName);
        Assert.Equal("ApiContainer", annotation.ClassName);
        Assert.Equal("API_CONTAINER", annotation.BindingName);
        Assert.Equal(8080, annotation.Port);
    }

    [Fact]
    public void PublishAsCloudflareContainer_Honors_Options()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        var cf = builder.AddCloudflareEnvironment();

        var api = AddApi(builder).PublishAsCloudflareContainer(cf, o =>
        {
            o.WorkerName = "orders";
            o.Port = 5000;
            o.MaxInstances = 10;
            o.InstanceType = "standard";
        });

        var annotation = Assert.Single(api.Resource.Annotations.OfType<CloudflareContainerAnnotation>());
        Assert.Equal("orders", annotation.WorkerName);
        Assert.Equal(5000, annotation.Port);
        Assert.Equal(10, annotation.MaxInstances);
        Assert.Equal("standard", annotation.InstanceType);
    }

    [Fact]
    public void PublishAsCloudflareContainer_Requires_Workers_Scope_And_Registers_Target()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        var cf = builder.AddCloudflareEnvironment();

        AddApi(builder).PublishAsCloudflareContainer(cf);

        Assert.Contains(CloudflareScopes.WorkersScriptsEdit, cf.Resource.RequiredScopes);
        Assert.Contains(builder.Services, d =>
            d.ServiceType == typeof(ICloudflarePublishTarget) &&
            d.ImplementationType == typeof(ContainerPublishTarget));
    }
}
