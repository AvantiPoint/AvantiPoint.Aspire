using Aspire.Hosting;
using AvantiPoint.Aspire.Cloudflare.R2;
using AvantiPoint.Aspire.Hosting.Cloudflare.R2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.R2.Tests;

public class R2AutoResolveAndClientTests
{
    [Fact]
    public void AddR2Bucket_Off_Builder_Resolves_Environment()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        var cf = builder.AddCloudflareEnvironment();

        var bucket = builder.AddR2Bucket("uploads");

        Assert.Same(cf.Resource, bucket.Resource.Environment);
    }

    [Fact]
    public void AddR2Client_Registers_IR2Client_Bound_To_Bucket()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["ConnectionStrings:uploads"] =
            "Endpoint=http://127.0.0.1:9000;AccessKey=ak;SecretKey=sk;Bucket=my-uploads;Region=auto";

        builder.AddR2Client("uploads");
        using var host = builder.Build();

        var r2 = host.Services.GetRequiredService<IR2Client>();
        Assert.Equal("my-uploads", r2.BucketName);
        Assert.NotNull(r2.S3);
    }
}
