using AvantiPoint.Aspire.Cloudflare.R2;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.R2.Tests;

public class R2ClientSettingsTests
{
    [Fact]
    public void ApplyConnectionString_Parses_All_Components()
    {
        var settings = new R2ClientSettings();

        settings.ApplyConnectionString(
            "Endpoint=https://acct.r2.cloudflarestorage.com;AccessKey=ak;SecretKey=sk;Bucket=uploads;Region=auto");

        Assert.Equal("https://acct.r2.cloudflarestorage.com", settings.Endpoint);
        Assert.Equal("ak", settings.AccessKey);
        Assert.Equal("sk", settings.SecretKey);
        Assert.Equal("uploads", settings.BucketName);
        Assert.Equal("auto", settings.Region);
    }

    [Fact]
    public void ApplyConnectionString_Ignores_Null_Or_Empty()
    {
        var settings = new R2ClientSettings { Endpoint = "keep" };

        settings.ApplyConnectionString(null);

        Assert.Equal("keep", settings.Endpoint);
    }

    [Fact]
    public void ApplyConnectionString_Handles_Emulator_Endpoint()
    {
        var settings = new R2ClientSettings();

        settings.ApplyConnectionString(
            "Endpoint=http://localhost:54321;AccessKey=cloudflare-r2-local;SecretKey=cloudflare-r2-local-secret;Bucket=uploads;Region=auto");

        Assert.Equal("http://localhost:54321", settings.Endpoint);
        Assert.Equal("cloudflare-r2-local", settings.AccessKey);
        Assert.Equal("uploads", settings.BucketName);
    }
}
