using Aspire.Hosting;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.D1;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.D1.Tests;

public class D1HostingTests
{
    private static IDistributedApplicationBuilder CreateRunModeBuilder()
        => DistributedApplication.CreateBuilder(Array.Empty<string>());

    [Fact]
    public void AddD1Database_Defaults_To_Real_HttpConnectionString()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        var db = cf.AddD1Database("catalog", databaseName: "catalog-db");

        Assert.False(db.Resource.UseEmulator);
        var expr = db.Resource.ConnectionStringExpression.ValueExpression;
        Assert.Contains("Provider=D1", expr);
        Assert.Contains("Database=catalog-db", expr);
        Assert.Contains("AccountId=", expr);
        Assert.Contains("Token=", expr);
    }

    [Fact]
    public void RunAsEmulator_Uses_Sqlite_ConnectionString()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        var db = cf.AddD1Database("catalog").RunAsEmulator();

        Assert.True(db.Resource.UseEmulator);
        var expr = db.Resource.ConnectionStringExpression.ValueExpression;
        Assert.Contains("Provider=Sqlite", expr);
        Assert.Contains("Data Source=", expr);
        Assert.Contains("catalog.db", expr);
    }

    [Fact]
    public void AddD1Database_Off_Builder_Resolves_Environment()
    {
        var builder = CreateRunModeBuilder();
        builder.AddCloudflareEnvironment();

        var db = builder.AddD1Database("catalog");

        Assert.Equal("catalog", db.Resource.DatabaseName);
        Assert.Single(builder.Resources.OfType<D1DatabaseResource>());
    }

    [Fact]
    public void AddD1Database_Requires_D1_Scope()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        cf.AddD1Database("catalog");

        Assert.Contains(CloudflareScopes.D1Edit, cf.Resource.RequiredScopes);
    }

    [Fact]
    public void AddD1Database_WithoutEnvironment_Throws()
    {
        var builder = CreateRunModeBuilder();

        Assert.Throws<InvalidOperationException>(() => builder.AddD1Database("catalog"));
    }
}
