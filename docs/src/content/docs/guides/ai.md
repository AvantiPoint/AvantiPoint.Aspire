---
title: "AI (Workers AI / AI Gateway)"
---


Cloudflare's AI endpoints — [Workers AI](https://developers.cloudflare.com/workers-ai/) and [AI Gateway](https://developers.cloudflare.com/ai-gateway/) — are OpenAI-compatible, and so is [Ollama](https://ollama.com/). The integration uses that to give you one [`Microsoft.Extensions.AI`](https://learn.microsoft.com/dotnet/ai/) `IChatClient`/`IEmbeddingGenerator` that runs against **local Ollama** during development and **Cloudflare** in production — the connection string just changes the endpoint and model.

## Add an AI resource (hosting)

```csharp
builder.AddCloudflareEnvironment();

var ai = builder.AddCloudflareAI("ai", o =>
{
    o.ChatModel = "@cf/meta/llama-3.1-8b-instruct"; // Cloudflare model (prod)
    o.EmbeddingModel = "@cf/baai/bge-base-en-v1.5";
    o.LocalChatModel = "llama3.2";                  // Ollama model (dev)
    o.LocalEmbeddingModel = "all-minilm";
}).RunAsEmulator();
```

- **`aspire run`** → with `.RunAsEmulator()`, a local **Ollama** server starts (shared across AI resources in the environment, with a persistent data volume) and pulls the configured local models. No Cloudflare credentials needed.
- **`aspire deploy`** → the same code talks to **Workers AI** (or an **AI Gateway**, see below). `.RunAsEmulator()` only affects run mode.

:::note
The first Ollama model pull downloads several GB. It is cached in a data volume, so you only pay it once.
:::

### Routing through an AI Gateway

Set a `GatewayId` to route through [AI Gateway](https://developers.cloudflare.com/ai-gateway/) — caching, logging, rate limiting, and multi-provider models (`anthropic/claude-*`, `openai/*`, `workers-ai/@cf/*`). The gateway is provisioned at deploy.

```csharp
var ai = builder.AddCloudflareAI("ai", o =>
{
    o.GatewayId = "my-gateway";
    o.ChatModel = "anthropic/claude-3-5-sonnet"; // a Claude model, via the gateway
}).RunAsEmulator();
```

## Reference it from a service

```csharp
builder.AddProject<Projects.Api>("api").WithReference(ai);
```

## Use it (client)

In the consuming project, register an `IChatClient` and/or `IEmbeddingGenerator`:

```csharp
builder.AddCloudflareAIChatClient("ai");
builder.AddCloudflareAIEmbeddingGenerator("ai");
```

Then inject the standard `Microsoft.Extensions.AI` abstractions — identical code locally and in production:

```csharp
app.MapPost("/chat", async (IChatClient chat, string prompt, CancellationToken ct) =>
{
    var response = await chat.GetResponseAsync(prompt, cancellationToken: ct);
    return Results.Ok(response.Text);
});

app.MapPost("/embed", async (IEmbeddingGenerator<string, Embedding<float>> embedder, string text, CancellationToken ct) =>
{
    var embedding = await embedder.GenerateVectorAsync(text, cancellationToken: ct);
    return Results.Ok(embedding.ToArray());
});
```

## How the endpoint is chosen

The client reads the connection string the AppHost injects:

| `Provider` | Endpoint | When |
| --- | --- | --- |
| `Ollama` | `http://…/v1` (local) | `aspire run` with `.RunAsEmulator()` |
| `WorkersAI` | `…/accounts/{id}/ai/v1` | Deployed, no gateway |
| `AIGateway` | `gateway.ai.cloudflare.com/v1/{id}/{gateway}/compat` | Deployed, with a gateway |

All three are OpenAI-compatible, so the `OpenAI`-backed `IChatClient`/`IEmbeddingGenerator` works against each unchanged.

## Credentials

- **API token** (runtime + gateway provisioning): needs *Workers AI: Read* (and *AI Gateway: Edit* if you use a gateway) — see [API Tokens](../getting-started/api-tokens.md). By default the client reuses the environment's Cloudflare API token; scope it with `.WithAccessToken(parameter)` if you prefer.
- **Local dev with `.RunAsEmulator()`:** none — Ollama runs locally with no key.
