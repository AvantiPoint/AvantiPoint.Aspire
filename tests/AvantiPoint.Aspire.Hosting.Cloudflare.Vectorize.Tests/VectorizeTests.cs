using Aspire.Hosting;
using AvantiPoint.Aspire.Cloudflare.Vectorize;
using AvantiPoint.Aspire.Cloudflare.Vectorize.Backends;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.Vectorize;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Vectorize.Tests;

public class VectorizeHostingTests
{
    private static IDistributedApplicationBuilder CreateRunModeBuilder()
        => DistributedApplication.CreateBuilder(Array.Empty<string>());

    [Fact]
    public void AddVectorizeIndex_Defaults_To_Real_ConnectionString()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        var index = cf.AddVectorizeIndex("docs", dimensions: 768, metric: VectorizeMetric.Cosine);

        Assert.False(index.Resource.UseEmulator);
        var expr = index.Resource.ConnectionStringExpression.ValueExpression;
        Assert.Contains("Provider=Vectorize", expr);
        Assert.Contains("Index=docs", expr);
        Assert.Contains("Dimensions=768", expr);
        Assert.Contains("Metric=cosine", expr);
        Assert.Contains("Token=", expr);
    }

    [Fact]
    public void RunAsEmulator_Uses_InMemory_ConnectionString()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        var index = cf.AddVectorizeIndex("docs", dimensions: 3, metric: VectorizeMetric.DotProduct).RunAsEmulator();

        Assert.True(index.Resource.UseEmulator);
        var expr = index.Resource.ConnectionStringExpression.ValueExpression;
        Assert.Contains("Provider=InMemory", expr);
        Assert.Contains("Metric=dot-product", expr);
    }

    [Fact]
    public void AddVectorizeIndex_Requires_Vectorize_Scope()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        cf.AddVectorizeIndex("docs", dimensions: 768);

        Assert.Contains(CloudflareScopes.VectorizeEdit, cf.Resource.RequiredScopes);
    }

    [Fact]
    public void AddVectorizeIndex_Rejects_NonPositive_Dimensions()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        Assert.Throws<ArgumentOutOfRangeException>(() => cf.AddVectorizeIndex("docs", dimensions: 0));
    }
}

public class VectorizeClientSettingsTests
{
    [Fact]
    public void ApplyConnectionString_Parses_InMemory()
    {
        var settings = new VectorizeClientSettings();
        settings.ApplyConnectionString("Provider=InMemory;Index=docs;Dimensions=384;Metric=cosine");

        Assert.True(settings.IsInMemory);
        Assert.Equal("docs", settings.IndexName);
        Assert.Equal(384, settings.Dimensions);
    }

    [Fact]
    public void ApplyConnectionString_Parses_Vectorize_Http()
    {
        var settings = new VectorizeClientSettings();
        settings.ApplyConnectionString("Provider=Vectorize;AccountId=acc;Index=docs;Dimensions=768;Metric=cosine;Token=tok");

        Assert.False(settings.IsInMemory);
        Assert.Equal("acc", settings.AccountId);
        Assert.Equal(768, settings.Dimensions);
        Assert.Equal("tok", settings.Token);
    }
}

public class InMemoryVectorizeBackendTests
{
    [Fact]
    public async Task Cosine_Query_Returns_Nearest_Vector_First()
    {
        var ct = TestContext.Current.CancellationToken;
        IVectorizeClient client = new VectorizeClient(new InMemoryVectorizeBackend("cosine", 3), "docs", 3);

        await client.UpsertAsync(
        [
            new VectorRecord("a", new float[] { 1f, 0f, 0f }),
            new VectorRecord("b", new float[] { 0f, 1f, 0f }),
            new VectorRecord("c", new float[] { 0.9f, 0.1f, 0f }),
        ], ct);

        var matches = await client.QueryAsync(new float[] { 1f, 0f, 0f }, topK: 2, cancellationToken: ct);

        Assert.Equal(2, matches.Count);
        Assert.Equal("a", matches[0].Id); // exact match ranks first
        Assert.Equal("c", matches[1].Id); // closest neighbour second
    }

    [Fact]
    public async Task Upsert_Get_And_Delete_RoundTrip()
    {
        var ct = TestContext.Current.CancellationToken;
        IVectorizeClient client = new VectorizeClient(new InMemoryVectorizeBackend("cosine", 2), "docs", 2);

        await client.UpsertAsync(
        [
            new VectorRecord("x", new float[] { 1f, 2f }, new Dictionary<string, object?> { ["tag"] = "t" }),
        ], ct);

        var fetched = await client.GetByIdsAsync(["x"], ct);
        Assert.Equal("t", Assert.Single(fetched).Metadata!["tag"]);

        await client.DeleteAsync(["x"], ct);
        Assert.Empty(await client.GetByIdsAsync(["x"], ct));
    }

    [Fact]
    public async Task Rejects_Vector_With_Wrong_Dimensions()
    {
        var ct = TestContext.Current.CancellationToken;
        IVectorizeClient client = new VectorizeClient(new InMemoryVectorizeBackend("cosine", 3), "docs", 3);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.UpsertAsync([new VectorRecord("a", new float[] { 1f, 0f })], ct));
    }
}
