---
title: "Queues"
---


[Cloudflare Queues](https://developers.cloudflare.com/queues/) is a message queue. The integration provisions a queue and gives you an `IQueueClient` to send and pull messages from .NET (using the HTTP push producer and pull consumer).

## Add a queue (hosting)

```csharp
builder.AddCloudflareEnvironment();
var jobs = builder.AddQueue("jobs").RunAsEmulator();
```

- **`aspire run`** → with `.RunAsEmulator()`, an in-process **in-memory** queue backs it (with lease/visibility semantics). No credentials needed.
- **`aspire deploy`** → the queue is provisioned in real Queues via the Cloudflare API.

## Reference it from a service

```csharp
builder.AddProject<Projects.Api>("api").WithReference(jobs);
```

## Use it (client)

```csharp
builder.AddQueueClient("jobs");
```

This registers an **`IQueueClient`** bound to the queue. Send messages, then pull and acknowledge them:

```csharp
// Producer
app.MapPost("/enqueue", async (string job, IQueueClient queue, CancellationToken ct) =>
{
    await queue.SendAsync(job, ct);
    return Results.Accepted();
});

// Consumer
app.MapPost("/process", async (IQueueClient queue, CancellationToken ct) =>
{
    var messages = await queue.PullAsync(batchSize: 10, cancellationToken: ct);
    foreach (var message in messages)
    {
        // ... handle message.Body ...
    }
    await queue.AckAsync(messages.Select(m => m.LeaseId), ct);
    return Results.Ok(new { processed = messages.Count });
});
```

Pulled messages are leased (hidden from other pulls) until you `AckAsync` (remove) or `RetryAsync` (return to the queue) them, or the lease expires.

:::note
Pulling against **real** Queues requires the queue to have an [HTTP pull consumer](https://developers.cloudflare.com/queues/configuration/pull-consumers/) configured. The in-memory emulator needs no setup.
:::

## How the backend is chosen

| `Provider` | Backend | When |
| --- | --- | --- |
| `Local` | In-process in-memory queue | `aspire run` with `.RunAsEmulator()` |
| `Queues` | The Queues HTTP API (push + pull) | Deployed |

## Credentials

- **API token**: needs *Queues: Edit* — see [API Tokens](../../getting-started/api-tokens/). The client reuses the environment's token by default; scope it with `.WithAccessToken(parameter)` if you prefer.
- **Local dev with `.RunAsEmulator()`:** none.
