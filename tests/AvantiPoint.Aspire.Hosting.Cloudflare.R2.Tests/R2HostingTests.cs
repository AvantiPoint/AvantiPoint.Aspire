using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.R2;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.R2.Tests;

public class R2HostingTests
{
    private static IDistributedApplicationBuilder CreateRunModeBuilder()
        => DistributedApplication.CreateBuilder(Array.Empty<string>());

    [Fact]
    public void AddR2Bucket_RunMode_Adds_Minio_Emulator_Container()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        cf.AddR2Bucket("uploads");

        var container = Assert.Single(builder.Resources.OfType<ContainerResource>());
        Assert.Contains("minio", container.Name, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddR2Bucket_RunMode_Uses_Emulator_ConnectionString()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        var bucket = cf.AddR2Bucket("uploads", bucketName: "my-uploads");

        Assert.True(bucket.Resource.UseEmulator);
        var expr = bucket.Resource.ConnectionStringExpression.ValueExpression;
        Assert.Contains("Endpoint=http://", expr);
        Assert.Contains("Bucket=my-uploads", expr);
        Assert.Contains("AccessKey=cloudflare-r2-local", expr);
        Assert.Contains("Region=auto", expr);
    }

    [Fact]
    public void Multiple_Buckets_Share_One_Minio_Emulator()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        cf.AddR2Bucket("uploads");
        cf.AddR2Bucket("thumbnails");

        Assert.Single(builder.Resources.OfType<ContainerResource>());
        Assert.Equal(2, builder.Resources.OfType<R2BucketResource>().Count());
    }

    [Fact]
    public void AddR2Bucket_Requires_R2_Scope()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        cf.AddR2Bucket("uploads");

        Assert.Contains(CloudflareScopes.WorkersR2StorageEdit, cf.Resource.RequiredScopes);
    }

    [Fact]
    public void RunAsReal_Switches_To_Real_Endpoint()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        var bucket = cf.AddR2Bucket("uploads").RunAsReal();

        Assert.False(bucket.Resource.UseEmulator);
        var expr = bucket.Resource.ConnectionStringExpression.ValueExpression;
        Assert.Contains("r2.cloudflarestorage.com", expr);
    }
}
