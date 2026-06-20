using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Tests;

public class CustomDomainTests
{
    private static IResourceBuilder<ProjectResource> AddApi(IDistributedApplicationBuilder builder)
        => builder.AddResource(new ProjectResource("api"));

    [Fact]
    public void WithCustomDomain_Adds_Annotation()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        var cf = builder.AddCloudflareEnvironment();
        var api = AddApi(builder).PublishAsCloudflareContainer(cf);

        api.WithCustomDomain("zone-123", "api.example.com");

        var domain = Assert.Single(api.Resource.Annotations.OfType<CustomDomainAnnotation>());
        Assert.Equal("zone-123", domain.ZoneId);
        Assert.Equal("api.example.com", domain.Hostname);
    }

    [Fact]
    public void WithCustomDomain_Requires_Dns_And_Zone_Scopes()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        var cf = builder.AddCloudflareEnvironment();
        AddApi(builder).PublishAsCloudflareContainer(cf).WithCustomDomain("zone-123", "api.example.com");

        Assert.Contains(CloudflareScopes.DnsRecordsEdit, cf.Resource.RequiredScopes);
        Assert.Contains(CloudflareScopes.ZoneRead, cf.Resource.RequiredScopes);
    }

    [Fact]
    public void WithCustomDomain_Is_Repeatable()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        var cf = builder.AddCloudflareEnvironment();

        var api = AddApi(builder).PublishAsCloudflareContainer(cf)
            .WithCustomDomain("zone-123", "api.example.com")
            .WithCustomDomain("zone-123", "api2.example.com");

        Assert.Equal(2, api.Resource.Annotations.OfType<CustomDomainAnnotation>().Count());
    }

    [Fact]
    public void WithCustomDomain_Before_PublishAs_Throws()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        AddApi(builder); // not yet targeting Cloudflare

        var api = builder.CreateResourceBuilder(builder.Resources.OfType<ProjectResource>().Single());

        Assert.Throws<InvalidOperationException>(() => api.WithCustomDomain("zone-123", "api.example.com"));
    }
}
