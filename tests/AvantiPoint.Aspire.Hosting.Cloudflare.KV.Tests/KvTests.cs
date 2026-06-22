using Aspire.Hosting;
using AvantiPoint.Aspire.Cloudflare.KV;
using AvantiPoint.Aspire.Cloudflare.KV.Backends;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.KV;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.KV.Tests;

public class KvHostingTests
{
    private static IDistributedApplicationBuilder CreateRunModeBuilder()
        => DistributedApplication.CreateBuilder(Array.Empty<string>());

    [Fact]
    public void AddKvNamespace_Defaults_To_Real_ConnectionString()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        var ns = cf.AddKvNamespace("cache", title: "cache-prod");

        Assert.False(ns.Resource.UseEmulator);
        var expr = ns.Resource.ConnectionStringExpression.ValueExpression;
        Assert.Contains("Provider=KV", expr);
        Assert.Contains("Namespace=cache-prod", expr);
        Assert.Contains("Token=", expr);
    }

    [Fact]
    public void RunAsEmulator_Uses_Local_ConnectionString()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        var ns = cf.AddKvNamespace("cache").RunAsEmulator();

        Assert.True(ns.Resource.UseEmulator);
        Assert.Contains("Provider=Local", ns.Resource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public void AddKvNamespace_Requires_KV_Scope()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        cf.AddKvNamespace("cache");

        Assert.Contains(CloudflareScopes.WorkersKVEdit, cf.Resource.RequiredScopes);
    }
}

public class KvClientSettingsTests
{
    [Fact]
    public void ApplyConnectionString_Parses_Local()
    {
        var settings = new KvClientSettings();
        settings.ApplyConnectionString("Provider=Local;Namespace=cache");

        Assert.True(settings.IsLocal);
        Assert.Equal("cache", settings.Namespace);
    }

    [Fact]
    public void ApplyConnectionString_Parses_KV_Http()
    {
        var settings = new KvClientSettings();
        settings.ApplyConnectionString("Provider=KV;AccountId=acc;Namespace=cache;Token=tok");

        Assert.False(settings.IsLocal);
        Assert.Equal("acc", settings.AccountId);
        Assert.Equal("cache", settings.Namespace);
        Assert.Equal("tok", settings.Token);
    }
}

public class InMemoryKvBackendTests
{
    [Fact]
    public async Task Put_Get_List_And_Delete_RoundTrip()
    {
        var ct = TestContext.Current.CancellationToken;
        ICloudflareKVClient client = new CloudflareKVClient(new InMemoryKvBackend(), "cache");

        await client.PutAsync("user:1", "alice", cancellationToken: ct);
        await client.PutAsync("user:2", "bob", cancellationToken: ct);

        Assert.Equal("alice", await client.GetStringAsync("user:1", ct));
        Assert.True(await client.ExistsAsync("user:2", ct));

        var keys = await client.ListKeysAsync("user:", ct);
        Assert.Equal(2, keys.Count);

        await client.DeleteAsync("user:1", ct);
        Assert.Null(await client.GetStringAsync("user:1", ct));
    }

    [Fact]
    public async Task Expired_Ttl_Returns_Null()
    {
        var ct = TestContext.Current.CancellationToken;
        ICloudflareKVClient client = new CloudflareKVClient(new InMemoryKvBackend(), "cache");

        await client.PutAsync("tmp", "x", TimeSpan.FromMilliseconds(-1), ct); // already expired
        Assert.Null(await client.GetStringAsync("tmp", ct));
    }
}
