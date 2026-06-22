using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.AI;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.AI.Tests;

public class CloudflareAIHostingTests
{
    private static IDistributedApplicationBuilder CreateRunModeBuilder()
        => DistributedApplication.CreateBuilder(Array.Empty<string>());

    [Fact]
    public void AddCloudflareAI_Defaults_To_WorkersAI_ConnectionString()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        var ai = cf.AddCloudflareAI("ai");

        Assert.False(ai.Resource.UseEmulator);
        var expr = ai.Resource.ConnectionStringExpression.ValueExpression;
        Assert.Contains("Provider=WorkersAI", expr);
        Assert.Contains("api.cloudflare.com/client/v4/accounts/", expr);
        Assert.Contains("/ai/v1", expr);
        Assert.Contains($"ChatModel={CloudflareAIOptions.DefaultCloudflareChatModel}", expr);
        Assert.Contains($"EmbeddingModel={CloudflareAIOptions.DefaultCloudflareEmbeddingModel}", expr);
    }

    [Fact]
    public void AddCloudflareAI_WithGateway_Uses_Compat_Endpoint()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        var ai = cf.AddCloudflareAI("ai", o =>
        {
            o.GatewayId = "my-gateway";
            o.ChatModel = "anthropic/claude-3-5-sonnet";
        });

        var expr = ai.Resource.ConnectionStringExpression.ValueExpression;
        Assert.Contains("Provider=AIGateway", expr);
        Assert.Contains("gateway.ai.cloudflare.com/v1/", expr);
        Assert.Contains("my-gateway/compat", expr);
        Assert.Contains("ChatModel=anthropic/claude-3-5-sonnet", expr);
        Assert.Contains(CloudflareScopes.AIGatewayEdit, cf.Resource.RequiredScopes);
    }

    [Fact]
    public void Gateway_Prefixes_Bare_WorkersAI_Models()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        // Default models are bare @cf/... ids; via a gateway they must be provider-prefixed.
        var ai = cf.AddCloudflareAI("ai", o => o.GatewayId = "gw");

        var expr = ai.Resource.ConnectionStringExpression.ValueExpression;
        Assert.Contains("ChatModel=workers-ai/@cf/", expr);
        Assert.Contains("EmbeddingModel=workers-ai/@cf/", expr);
    }

    [Fact]
    public void RunAsEmulator_Adds_Ollama_And_Uses_Ollama_ConnectionString()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        var ai = cf.AddCloudflareAI("ai", o =>
        {
            o.LocalChatModel = "llama3.2";
            o.LocalEmbeddingModel = "all-minilm";
        }).RunAsEmulator();

        Assert.True(ai.Resource.UseEmulator);
        Assert.Single(builder.Resources.OfType<OllamaResource>());

        var expr = ai.Resource.ConnectionStringExpression.ValueExpression;
        Assert.Contains("Provider=Ollama", expr);
        Assert.Contains("/v1", expr);
        Assert.Contains("ChatModel=llama3.2", expr);
        Assert.Contains("EmbeddingModel=all-minilm", expr);
    }

    [Fact]
    public void RunOnHost_Uses_Host_Ollama_And_Ollama_ConnectionString()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        var ai = cf.AddCloudflareAI("ai").RunOnHost();

        Assert.True(ai.Resource.UseEmulator);
        // A host (executable) Ollama resource is added, not a container.
        Assert.Single(builder.Resources.OfType<OllamaExecutableResource>());
        Assert.Empty(builder.Resources.OfType<OllamaResource>());
        Assert.Contains("Provider=Ollama", ai.Resource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public void Multiple_AI_Resources_Share_One_Ollama()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        cf.AddCloudflareAI("chat").RunAsEmulator();
        cf.AddCloudflareAI("rag").RunAsEmulator();

        Assert.Single(builder.Resources.OfType<OllamaResource>());
    }

    [Fact]
    public void AddCloudflareAI_Requires_WorkersAI_Scope()
    {
        var builder = CreateRunModeBuilder();
        var cf = builder.AddCloudflareEnvironment();

        cf.AddCloudflareAI("ai");

        Assert.Contains(CloudflareScopes.WorkersAIRead, cf.Resource.RequiredScopes);
    }

    [Fact]
    public void AddCloudflareAI_Off_Builder_Resolves_Environment()
    {
        var builder = CreateRunModeBuilder();
        builder.AddCloudflareEnvironment();

        var ai = builder.AddCloudflareAI("ai");

        Assert.Single(builder.Resources.OfType<CloudflareAIResource>());
        Assert.Equal("ai", ai.Resource.Name);
    }
}
