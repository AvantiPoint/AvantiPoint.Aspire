---
title: "Workers KV"
---


[Workers KV](https://developers.cloudflare.com/kv/) is Cloudflare's low-latency key-value store. The integration provisions a KV namespace and gives you an `ICloudflareKVClient` to read and write it from .NET.

## Add a namespace (hosting)

```csharp
builder.AddCloudflareEnvironment();
var cache = builder.AddKvNamespace("cache").RunAsEmulator();
```

- **`aspire run`** → with `.RunAsEmulator()`, an in-process **in-memory** store backs the namespace. No credentials needed.
- **`aspire deploy`** → the namespace is provisioned in real KV via the Cloudflare API.

## Reference it from a service

```csharp
builder.AddProject<Projects.Api>("api").WithReference(cache);
```

## Use it (client)

```csharp
builder.AddKvClient("cache");
```

This registers an **`ICloudflareKVClient`** bound to the namespace. The same code runs against the in-memory store locally and the Workers KV HTTP API in production:

```csharp
app.MapGet("/cache/{key}", async (string key, ICloudflareKVClient kv, CancellationToken ct) =>
    await kv.GetStringAsync(key, ct) is { } value ? Results.Ok(value) : Results.NotFound());

app.MapPut("/cache/{key}", async (string key, string value, ICloudflareKVClient kv, CancellationToken ct) =>
{
    await kv.PutAsync(key, value, expirationTtl: TimeSpan.FromHours(1), ct);
    return Results.NoContent();
});
```

`ICloudflareKVClient` also exposes `ExistsAsync`, `DeleteAsync`, and `ListKeysAsync(prefix)`.

## How the backend is chosen

| `Provider` | Backend | When |
| --- | --- | --- |
| `Local` | In-process in-memory store | `aspire run` with `.RunAsEmulator()` |
| `KV` | The Workers KV HTTP API | Deployed |

The HTTP backend resolves the namespace id from its title on first use, so you never hard-code a namespace id.

## Credentials

- **API token**: needs *Workers KV Storage: Edit* — see [API Tokens](../../getting-started/api-tokens/). The client reuses the environment's token by default; scope it with `.WithAccessToken(parameter)` if you prefer.
- **Local dev with `.RunAsEmulator()`:** none.
