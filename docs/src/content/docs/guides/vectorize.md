---
title: "Vectorize"
---


[Vectorize](https://developers.cloudflare.com/vectorize/) is Cloudflare's vector database — store embeddings and query for nearest neighbours (semantic search, RAG). It pairs naturally with the [AI](ai.md) integration: generate embeddings with `IEmbeddingGenerator`, store and search them with `IVectorizeClient`.

## Add an index (hosting)

```csharp
builder.AddCloudflareEnvironment();

// dimensions must match your embedding model (e.g. 768 for @cf/baai/bge-base-en-v1.5).
var docs = builder.AddVectorizeIndex("docs", dimensions: 768, metric: VectorizeMetric.Cosine)
    .RunAsEmulator();
```

- **`aspire run`** → with `.RunAsEmulator()`, an in-process **in-memory** vector store backs the index. No credentials needed.
- **`aspire deploy`** → the index is provisioned in real Vectorize via the Cloudflare API.

Supported metrics: `Cosine` (default), `Euclidean`, `DotProduct`.

## Reference it from a service

```csharp
builder.AddProject<Projects.Api>("api").WithReference(docs);
```

## Use it (client)

```csharp
builder.AddVectorizeClient("docs");
```

This registers an **`IVectorizeClient`** bound to the index. The same code runs against the in-memory store locally and the Vectorize v2 HTTP API in production:

```csharp
app.MapPost("/index", async (IVectorizeClient vectors, IEmbeddingGenerator<string, Embedding<float>> embedder, Doc doc, CancellationToken ct) =>
{
    var vector = await embedder.GenerateVectorAsync(doc.Text, cancellationToken: ct);
    await vectors.UpsertAsync(
    [
        new VectorRecord(doc.Id, vector, new Dictionary<string, object?> { ["title"] = doc.Title }),
    ], ct);
    return Results.Ok();
});

app.MapGet("/search", async (string q, IVectorizeClient vectors, IEmbeddingGenerator<string, Embedding<float>> embedder, CancellationToken ct) =>
{
    var query = await embedder.GenerateVectorAsync(q, cancellationToken: ct);
    var matches = await vectors.QueryAsync(query, topK: 5, cancellationToken: ct);
    return Results.Ok(matches.Select(m => new { m.Id, m.Score, m.Metadata }));
});
```

`IVectorizeClient` also exposes `GetByIdsAsync` and `DeleteAsync`.

:::note
Real Vectorize mutations (upsert/delete) are **asynchronous (eventually consistent)** — a query may not immediately reflect a just-completed upsert. The in-memory emulator is synchronous, so the dev loop is immediate.
:::

## How the backend is chosen

| `Provider` | Backend | When |
| --- | --- | --- |
| `InMemory` | In-process vector store | `aspire run` with `.RunAsEmulator()` |
| `Vectorize` | The Vectorize v2 HTTP API | Deployed |

## Credentials

- **API token**: needs *Vectorize: Edit* — see [API Tokens](../getting-started/api-tokens.md). By default the client reuses the environment's Cloudflare API token; scope it with `.WithAccessToken(parameter)` if you prefer.
- **Local dev with `.RunAsEmulator()`:** none.
