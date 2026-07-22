using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.R2;
using AvantiPoint.Aspire.Hosting.Cloudflare.R2.Emulator;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.R2.Tests;

public class R2HostingTests
{
    private static IDistributedApplicationBuilder CreateRunModeBuilder()
        => DistributedApplication.CreateBuilder(Array.Empty<string>());

    [Fact]
    public void RunAsEmulator_Adds_Minio_Emulator_Container()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        cf.AddR2Bucket("uploads").RunAsEmulator();

        var container = Assert.Single(builder.Resources.OfType<ContainerResource>());
        Assert.Contains("minio", container.Name, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RunAsEmulator_Uses_Emulator_ConnectionString()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        var bucket = cf.AddR2Bucket("uploads", bucketName: "my-uploads").RunAsEmulator();

        Assert.True(bucket.Resource.UseEmulator);
        var expr = bucket.Resource.ConnectionStringExpression.ValueExpression;
        Assert.Contains("Endpoint=http://", expr);
        Assert.Contains("Bucket=my-uploads", expr);
        Assert.Contains("AccessKey=cloudflare-r2-local", expr);
        Assert.Contains("Region=auto", expr);
    }

    [Fact]
    public void AddR2Bucket_Defaults_To_Real_Endpoint()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        var bucket = cf.AddR2Bucket("uploads");

        Assert.False(bucket.Resource.UseEmulator);
        Assert.Empty(builder.Resources.OfType<ContainerResource>()); // no MinIO unless RunAsEmulator()
        var expr = bucket.Resource.ConnectionStringExpression.ValueExpression;
        Assert.Contains("r2.cloudflarestorage.com", expr);
    }

    [Fact]
    public void Multiple_Emulated_Buckets_Share_One_Minio_Emulator()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        cf.AddR2Bucket("uploads").RunAsEmulator();
        cf.AddR2Bucket("thumbnails").RunAsEmulator();

        Assert.Single(builder.Resources.OfType<ContainerResource>());
        Assert.Equal(2, builder.Resources.OfType<R2BucketResource>().Count());
    }

    [Fact]
    public void RunAsEmulator_Tracks_Logical_Bucket_Resource_For_Readiness()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        var bucket = cf.AddR2Bucket("uploads").RunAsEmulator();

        Assert.True(cf.Resource.TryGetLastAnnotation<MinioEmulatorAnnotation>(out var emulator));
        Assert.Contains(bucket.Resource, emulator.Buckets);
    }

    [Fact]
    public void AddR2Bucket_Requires_R2_Scope()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        cf.AddR2Bucket("uploads");

        Assert.Contains(CloudflareScopes.WorkersR2StorageEdit, cf.Resource.RequiredScopes);
    }
}
