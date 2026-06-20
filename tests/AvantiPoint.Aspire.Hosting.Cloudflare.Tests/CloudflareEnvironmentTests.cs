using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Tests;

public class CloudflareEnvironmentTests
{
    [Fact]
    public void AddCloudflareEnvironment_Adds_Compute_Environment_Resource()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());

        var cf = builder.AddCloudflareEnvironment();

        var resource = Assert.Single(builder.Resources.OfType<CloudflareEnvironmentResource>());
        Assert.Same(cf.Resource, resource);
        Assert.IsAssignableFrom<IComputeEnvironmentResource>(resource);
    }

    [Fact]
    public void AddCloudflareEnvironment_Creates_Token_And_Account_Parameters()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());

        var cf = builder.AddCloudflareEnvironment();

        Assert.True(cf.Resource.ApiToken.Secret);
        Assert.False(cf.Resource.AccountId.Secret);
    }

    [Fact]
    public void AddCloudflareEnvironment_Registers_Api_Client()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());

        builder.AddCloudflareEnvironment();

        Assert.Contains(builder.Services, d => d.ServiceType == typeof(ICloudflareApiClient));
    }

    [Fact]
    public void AddCloudflareEnvironment_Always_Requires_UserDetailsRead_Scope()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());

        var cf = builder.AddCloudflareEnvironment();

        Assert.Contains(CloudflareScopes.UserDetailsRead, cf.Resource.RequiredScopes);
    }

    [Fact]
    public void AddCloudflareHostingServices_Is_Idempotent()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());

        builder.AddCloudflareEnvironment("one");
        builder.AddCloudflareEnvironment("two");

        Assert.Single(builder.Services, d => d.ServiceType == typeof(ICloudflareApiClient));
    }
}
