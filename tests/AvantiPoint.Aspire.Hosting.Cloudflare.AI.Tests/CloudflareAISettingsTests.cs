using AvantiPoint.Aspire.Cloudflare.AI;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.AI.Tests;

public class CloudflareAISettingsTests
{
    [Fact]
    public void ApplyConnectionString_Parses_WorkersAI()
    {
        var settings = new CloudflareAISettings();
        settings.ApplyConnectionString(
            "Provider=WorkersAI;Endpoint=https://api.cloudflare.com/client/v4/accounts/acc/ai/v1;Key=tok;ChatModel=@cf/meta/llama-3.1-8b-instruct;EmbeddingModel=@cf/baai/bge-base-en-v1.5");

        Assert.Equal("WorkersAI", settings.Provider);
        Assert.Equal("https://api.cloudflare.com/client/v4/accounts/acc/ai/v1", settings.Endpoint);
        Assert.Equal("tok", settings.Key);
        Assert.Equal("@cf/meta/llama-3.1-8b-instruct", settings.ChatModel);
        Assert.Equal("@cf/baai/bge-base-en-v1.5", settings.EmbeddingModel);
    }

    [Fact]
    public void ApplyConnectionString_Parses_Ollama_Without_Key()
    {
        var settings = new CloudflareAISettings();
        settings.ApplyConnectionString(
            "Provider=Ollama;Endpoint=http://localhost:11434/v1;ChatModel=llama3.2;EmbeddingModel=all-minilm");

        Assert.Equal("Ollama", settings.Provider);
        Assert.Equal("http://localhost:11434/v1", settings.Endpoint);
        Assert.True(string.IsNullOrEmpty(settings.Key));
        Assert.Equal("llama3.2", settings.ChatModel);
    }
}
