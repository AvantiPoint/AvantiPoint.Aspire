---
title: "Cloudflare Workers"
---


Run and deploy a hand-authored [Cloudflare Worker](https://developers.cloudflare.com/workers/) (its own `wrangler.toml`/`wrangler.jsonc` + source) as part of your Aspire app.

```csharp
builder.AddCloudflareEnvironment();

builder.AddCloudflareWorker("worker", "../MyWorker");
```

- **`aspire run`** → the Worker runs **locally** with `wrangler dev` (Cloudflare's Miniflare runtime) — **no Cloudflare account required**. It appears in the Aspire dashboard like any other resource.
- **`aspire deploy`** → the Worker is deployed with `wrangler deploy`.

## When to use this vs. Containers

| Use | For |
| --- | --- |
| `AddCloudflareWorker` | A Worker you author in JavaScript/TypeScript, with its own `wrangler` config. |
| `PublishAsCloudflareContainer` | A **.NET** project you want to run as a container (a Worker + Durable Object shim is generated for you). |

## Options

```csharp
builder.AddCloudflareWorker("worker", "../MyWorker", options =>
{
    options.Port = 8787; // the local `wrangler dev` port during `aspire run`
});
```

## Bindings, routes & custom domains

A hand-authored Worker owns its configuration, so add **R2/KV/D1 bindings, routes and custom domains in the Worker's own `wrangler.jsonc`** — that's the idiomatic place for them and it keeps `wrangler dev` and `wrangler deploy` in agreement. For example:

```jsonc
{
  "name": "my-worker",
  "main": "src/index.js",
  "compatibility_date": "2025-06-01",
  "r2_buckets": [{ "binding": "UPLOADS", "bucket_name": "uploads" }],
  "routes": [{ "pattern": "api.example.com/*", "zone_id": "<ZONE_ID>" }]
}
```

## Requirements

- **wrangler** and **Node.js** installed — see [How wrangler authenticates](../../getting-started/wrangler/).
- For deploy: an API token with *Workers Scripts: Edit* — see [API Tokens](../../getting-started/api-tokens/).
- Local `wrangler dev` needs **no** Cloudflare credentials.
