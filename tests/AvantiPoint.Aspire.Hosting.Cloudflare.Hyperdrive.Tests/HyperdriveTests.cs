using Aspire.Hosting;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.Hyperdrive;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Hyperdrive.Tests;

public class PostgresConnectionStringParserTests
{
    [Fact]
    public void Parses_Uri_Form()
    {
        var origin = PostgresConnectionStringParser.Parse("postgresql://alice:s3cret@db.example.com:6543/orders");

        Assert.Equal("postgres", origin.Scheme);
        Assert.Equal("db.example.com", origin.Host);
        Assert.Equal(6543, origin.Port);
        Assert.Equal("orders", origin.Database);
        Assert.Equal("alice", origin.User);
        Assert.Equal("s3cret", origin.Password);
    }

    [Fact]
    public void Parses_KeyValue_Form()
    {
        var origin = PostgresConnectionStringParser.Parse(
            "Host=db.example.com;Port=5432;Database=orders;Username=alice;Password=s3cret");

        Assert.Equal("db.example.com", origin.Host);
        Assert.Equal(5432, origin.Port);
        Assert.Equal("orders", origin.Database);
        Assert.Equal("alice", origin.User);
        Assert.Equal("s3cret", origin.Password);
    }

    [Fact]
    public void Defaults_Port_To_5432()
    {
        var origin = PostgresConnectionStringParser.Parse("Host=h;Database=d;Username=u;Password=p");
        Assert.Equal(5432, origin.Port);
    }

    [Fact]
    public void Throws_On_Empty()
    {
        Assert.Throws<ArgumentException>(() => PostgresConnectionStringParser.Parse(""));
    }
}

public class HyperdriveHostingTests
{
    private static IDistributedApplicationBuilder CreatePublishModeBuilder()
        => DistributedApplication.CreateBuilder(["--operation", "publish", "--output-path", "."]);

    [Fact]
    public void PublishAsHyperdrive_Creates_Resource_And_Requires_Scope()
    {
        var builder = CreatePublishModeBuilder();
        var cf = builder.AddCloudflareEnvironment();
        var prod = builder.AddParameter("pg-prod", "Host=prod;Port=5432;Database=app;Username=u;Password=p", secret: true);

        var pg = builder.AddConnectionString("pg");
        var hd = pg.PublishAsHyperdrive("hd", prod);

        Assert.Equal("hd", hd.Resource.Name);
        Assert.Same(cf.Resource, hd.Resource.Environment);
        Assert.Contains(CloudflareScopes.HyperdriveEdit, cf.Resource.RequiredScopes);
        Assert.Single(builder.Resources.OfType<HyperdriveResource>());
    }

    [Fact]
    public void WithHyperdrive_Records_Worker_Binding()
    {
        var builder = CreatePublishModeBuilder();
        builder.AddCloudflareEnvironment();
        var prod = builder.AddParameter("pg-prod", "Host=prod;Port=5432;Database=app;Username=u;Password=p", secret: true);

        var hd = builder.AddConnectionString("pg").PublishAsHyperdrive("hd", prod);
        builder.AddCloudflareWorker("api-worker", "../worker").WithHyperdrive(hd, bindingName: "DB");

        Assert.Contains(hd.Resource.Bindings, b => b.WorkerName == "api-worker" && b.BindingName == "DB");
    }
}
