using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;
using AvantiPoint.Aspire.Hosting.Cloudflare.R2;
using AvantiPoint.Aspire.Hosting.Cloudflare.R2.Publishing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.R2.Tests;

public class R2PublishTargetTests
{
    [Fact]
    public void AddR2Bucket_Registers_R2_Publish_Target()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        var cf = builder.AddCloudflareEnvironment();

        cf.AddR2Bucket("uploads");

        Assert.Contains(builder.Services, d =>
            d.ServiceType == typeof(ICloudflarePublishTarget) &&
            d.ImplementationType == typeof(R2PublishTarget));
    }

    [Fact]
    public void Publish_Target_Registration_Is_Deduplicated()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        var cf = builder.AddCloudflareEnvironment();

        cf.AddR2Bucket("a");
        cf.AddR2Bucket("b");

        Assert.Single(builder.Services, d =>
            d.ServiceType == typeof(ICloudflarePublishTarget) &&
            d.ImplementationType == typeof(R2PublishTarget));
    }

    [Fact]
    public void R2PublishTarget_Only_Handles_R2Buckets()
    {
        var target = new R2PublishTarget(NullLogger<R2PublishTarget>.Instance);

        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        var cf = builder.AddCloudflareEnvironment();
        var bucket = cf.AddR2Bucket("uploads").Resource;
        var container = builder.AddContainer("other", "nginx").Resource;

        Assert.True(target.CanHandle(bucket));
        Assert.False(target.CanHandle(container));
    }

    [Fact]
    public void AllowDeletion_Opts_Into_Destroy()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        var cf = builder.AddCloudflareEnvironment();

        var bucket = cf.AddR2Bucket("uploads");
        Assert.False(bucket.Resource.AllowDestroy);

        bucket.AllowDeletion();
        Assert.True(bucket.Resource.AllowDestroy);
    }
}
