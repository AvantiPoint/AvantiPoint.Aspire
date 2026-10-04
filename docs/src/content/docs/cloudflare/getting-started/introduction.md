---
title: "Cloudflare Integration Introduction"
description: "Model Cloudflare resources in an Aspire AppHost with C# or TypeScript, then provision and deploy them through the shared hosting integrations."
---


The **Cloudflare** integration (part of [AvantiPoint Aspire](../../../overview/)) lets you model your Cloudflare resources in your Aspire AppHost and deploy them with `aspire deploy`.

## How it works

AppHost examples provide synchronized **C#** and **TypeScript** tabs. Both use the same .NET hosting integrations and deployment behavior. See the [quickstart](../quickstart/) for package setup and builder initialization. The consuming .NET service-client examples remain C#; these packages do not provide JavaScript runtime clients.

When you add a **Cloudflare environment** to your AppHost, the integration hooks into Aspire's publish/deploy pipeline:

1. **Validate** - your Cloudflare API token is checked up front; deployment fails fast with a clear message if it's missing, invalid, or inactive. Permission checks happen in the service APIs during deployment.
2. **Publish** — build artifacts are produced (container images, generated `wrangler.jsonc`, the JS build output).
3. **Deploy** — resources are created/updated on Cloudflare: data services (R2, D1, KV, Queues, Vectorize, Hyperdrive, AI Gateway) via the REST API; Workers, Containers and Pages via `wrangler`.

During local `aspire run`, services that support `.RunAsEmulator()` run against a local emulator instead of Cloudflare — R2 against **MinIO**, D1 against **SQLite**, AI against **Ollama**, and KV/Queues/Vectorize against in-memory stores — so the inner dev loop needs no Cloudflare credentials at all.

## What's supported

Each service ships a **hosting** package (model + provision) and, where a .NET app consumes it, a matching **client** package:

| Service | Client | Local emulator |
| --- | --- | --- |
| [R2](../../guides/r2/) | `IR2Client` (S3-compatible) | MinIO |
| [D1](../../guides/d1/) | `ID1Client` | SQLite |
| [AI](../../guides/ai/) | `IChatClient` / `IEmbeddingGenerator` | Ollama |
| [Vectorize](../../guides/vectorize/) | `IVectorizeClient` | in-memory |
| [Workers KV](../../guides/kv/) | `ICloudflareKVClient` | in-memory |
| [Queues](../../guides/queues/) | `IQueueClient` | in-memory |
| [Hyperdrive](../../guides/hyperdrive/) | *(Worker binding — no .NET client)* | your normal database |
| [Workers](../../guides/workers/) / [Containers](../../guides/containers/) / [Pages](../../guides/pages/) | — | runs locally |

Plus [custom domains](../../guides/custom-domains/) for Containers, Workers, and Pages.

## What you'll set up

Most of the friction in getting started is on the Cloudflare side — gathering identifiers and minting a token with the right permissions. The next pages walk through each piece:

| Step | Why |
| --- | --- |
| [Prerequisites](../prerequisites/) | Tools you need installed. |
| [Account ID & Zone ID](../account-and-zone-ids/) | Required for provisioning and custom domains. |
| [API Tokens & Scopes](../api-tokens/) | The token the integration and `wrangler` use — scoped to the services you deploy. |
| [R2 S3 Credentials](../r2-credentials/) | Separate keys your app uses to read/write R2 at runtime. |
| [How wrangler authenticates](../wrangler/) | Non-interactive auth for Pages/Containers. |
| [Quickstart](../quickstart/) | Wire it all together. |
