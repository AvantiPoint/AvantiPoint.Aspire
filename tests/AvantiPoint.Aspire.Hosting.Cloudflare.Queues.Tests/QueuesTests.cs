using Aspire.Hosting;
using AvantiPoint.Aspire.Cloudflare.Queues;
using AvantiPoint.Aspire.Cloudflare.Queues.Backends;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.Queues;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Queues.Tests;

public class QueuesHostingTests
{
    private static IDistributedApplicationBuilder CreateRunModeBuilder()
        => DistributedApplication.CreateBuilder(Array.Empty<string>());

    [Fact]
    public void AddQueue_Defaults_To_Real_ConnectionString()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        var queue = cf.AddQueue("jobs", queueName: "jobs-prod");

        Assert.False(queue.Resource.UseEmulator);
        var expr = queue.Resource.ConnectionStringExpression.ValueExpression;
        Assert.Contains("Provider=Queues", expr);
        Assert.Contains("Queue=jobs-prod", expr);
        Assert.Contains("Token=", expr);
    }

    [Fact]
    public void RunAsEmulator_Uses_Local_ConnectionString()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        var queue = cf.AddQueue("jobs").RunAsEmulator();

        Assert.True(queue.Resource.UseEmulator);
        Assert.Contains("Provider=Local", queue.Resource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public void AddQueue_Requires_Queues_Scope()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        cf.AddQueue("jobs");

        Assert.Contains(CloudflareScopes.QueuesEdit, cf.Resource.RequiredScopes);
    }
}

public class QueueClientSettingsTests
{
    [Fact]
    public void ApplyConnectionString_Parses_Local()
    {
        var settings = new QueueClientSettings();
        settings.ApplyConnectionString("Provider=Local;Queue=jobs");

        Assert.True(settings.IsLocal);
        Assert.Equal("jobs", settings.QueueName);
    }

    [Fact]
    public void ApplyConnectionString_Parses_Queues_Http()
    {
        var settings = new QueueClientSettings();
        settings.ApplyConnectionString("Provider=Queues;AccountId=acc;Queue=jobs;Token=tok");

        Assert.False(settings.IsLocal);
        Assert.Equal("acc", settings.AccountId);
        Assert.Equal("jobs", settings.QueueName);
    }
}

public class InMemoryQueueBackendTests
{
    [Fact]
    public async Task Send_Pull_And_Ack_RoundTrip()
    {
        var ct = TestContext.Current.CancellationToken;
        IQueueClient client = new QueueClient(new InMemoryQueueBackend(), "jobs");

        await client.SendAsync("job-1", ct);
        await client.SendAsync("job-2", ct);

        var messages = await client.PullAsync(batchSize: 10, cancellationToken: ct);
        Assert.Equal(2, messages.Count);
        Assert.Equal(1, messages[0].Attempts);

        // Leased messages are hidden from a second pull.
        Assert.Empty(await client.PullAsync(batchSize: 10, cancellationToken: ct));

        await client.AckAsync(messages.Select(m => m.LeaseId), ct);

        // After ack the queue is empty.
        Assert.Empty(await client.PullAsync(batchSize: 10, cancellationToken: ct));
    }

    [Fact]
    public async Task Retry_Makes_Message_Visible_Again()
    {
        var ct = TestContext.Current.CancellationToken;
        IQueueClient client = new QueueClient(new InMemoryQueueBackend(), "jobs");

        await client.SendAsync("job-1", ct);
        var first = await client.PullAsync(batchSize: 10, cancellationToken: ct);
        await client.RetryAsync(first.Select(m => m.LeaseId), ct);

        var second = await client.PullAsync(batchSize: 10, cancellationToken: ct);
        Assert.Single(second);
        Assert.Equal(2, second[0].Attempts); // redelivered
    }
}
