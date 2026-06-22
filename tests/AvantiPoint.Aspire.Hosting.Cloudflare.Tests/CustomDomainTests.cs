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
    public async Task WithCustomDomain_Adds_Annotation_With_Literal_ZoneId()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        builder.AddCloudflareEnvironment();
        var api = AddApi(builder).PublishAsCloudflareContainer();

        api.WithCustomDomain("api.example.com", "zone-123");

        var domain = Assert.Single(api.Resource.Annotations.OfType<CustomDomainAnnotation>());
        Assert.Equal("api.example.com", domain.Hostname);
        Assert.Equal("zone-123", await domain.GetZoneIdAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task WithCustomDomain_Accepts_ZoneId_Parameter()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        builder.AddCloudflareEnvironment();
        var zoneId = builder.AddParameter("zone-id", "zone-from-param");
        var api = AddApi(builder).PublishAsCloudflareContainer();

        api.WithCustomDomain("api.example.com", zoneId);

        var domain = Assert.Single(api.Resource.Annotations.OfType<CustomDomainAnnotation>());
        Assert.Equal("zone-from-param", await domain.GetZoneIdAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void WithCustomDomain_Requires_Dns_And_Zone_Scopes()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        var cf = builder.AddCloudflareEnvironment();
        AddApi(builder).PublishAsCloudflareContainer().WithCustomDomain("api.example.com", "zone-123");

        Assert.Contains(CloudflareScopes.DnsRecordsEdit, cf.Resource.RequiredScopes);
        Assert.Contains(CloudflareScopes.ZoneRead, cf.Resource.RequiredScopes);
    }

    [Fact]
    public void WithCustomDomain_Is_Repeatable()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        builder.AddCloudflareEnvironment();

        var api = AddApi(builder).PublishAsCloudflareContainer()
            .WithCustomDomain("api.example.com", "zone-123")
            .WithCustomDomain("api2.example.com", "zone-123");

        Assert.Equal(2, api.Resource.Annotations.OfType<CustomDomainAnnotation>().Count());
    }

    [Fact]
    public void WithCustomDomain_Before_PublishAs_Throws()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        AddApi(builder); // not yet targeting Cloudflare

        var api = builder.CreateResourceBuilder(builder.Resources.OfType<ProjectResource>().Single());

        Assert.Throws<InvalidOperationException>(() => api.WithCustomDomain("api.example.com", "zone-123"));
    }
}
