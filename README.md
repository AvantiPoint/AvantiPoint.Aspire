# AvantiPoint Aspire for Cloudflare

[![CI](https://github.com/AvantiPoint/AvantiPoint.Aspire/actions/workflows/ci.yml/badge.svg)](https://github.com/AvantiPoint/AvantiPoint.Aspire/actions/workflows/ci.yml)
[![Docs](https://github.com/AvantiPoint/AvantiPoint.Aspire/actions/workflows/docs.yml/badge.svg)](https://github.com/AvantiPoint/AvantiPoint.Aspire/actions/workflows/docs.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

📖 **[Documentation & getting-started guides](https://avantipoint.github.io/AvantiPoint.Aspire/)** — finding your Account/Zone IDs, scoping API tokens, R2 credentials, wrangler auth, and more.

Hosting and client integrations that make [Aspire](https://aspire.dev) a first-class way to **provision and deploy to [Cloudflare](https://www.cloudflare.com/)** — the same way the AWS integration targets AWS, instead of Azure.

`aspire deploy` is hijacked so your distributed application is published and deployed to Cloudflare:

- **.NET APIs → Cloudflare Containers** — a container image is built from your `ProjectResource` and run on Cloudflare Containers.
- **Cloudflare Workers** — run a hand-authored Worker locally with `wrangler dev` (no account needed) and deploy it with `aspire deploy`.
- **JavaScript frontends → Cloudflare Pages** — your build output directory is deployed to Pages.
- **R2 buckets** — provisioned via the Cloudflare API, with S3-compatible connection details flowed back to your apps. Add `.RunAsEmulator()` and a local **MinIO** emulator backs the bucket during `aspire run`, so the inner dev loop needs no cloud credentials.
- **D1 databases** — provisioned via the Cloudflare API, queried from .NET through `ID1Client`. Add `.RunAsEmulator()` and the database is a local **SQLite** file during `aspire run` (D1 is SQLite under the hood); deployed, the same query code runs against the D1 HTTP API.
- **AI (Workers AI / AI Gateway)** — a `Microsoft.Extensions.AI` `IChatClient`/`IEmbeddingGenerator` over Cloudflare's OpenAI-compatible AI endpoints. Add `.RunAsEmulator()` and a local **Ollama** server backs it during `aspire run`; deployed, the same code runs against Workers AI or an AI Gateway (incl. `anthropic/claude-*`).
- **Vectorize** — a vector database for embeddings/RAG, used from .NET through `IVectorizeClient` (upsert/query/delete). Add `.RunAsEmulator()` for an in-memory store during `aspire run`; deployed, the same code runs against the Vectorize v2 HTTP API.
- **Workers KV** — a key-value store, used from .NET through `ICloudflareKVClient` (get/put/delete/list). `.RunAsEmulator()` gives an in-memory store during `aspire run`; deployed, the Workers KV HTTP API.
- **Queues** — a message queue, used from .NET through `IQueueClient` (send/pull/ack/retry). `.RunAsEmulator()` gives an in-memory queue during `aspire run`; deployed, the Queues HTTP API.
- **Hyperdrive** — accelerates a production (e.g. third-party) database for Workers. `PublishAsHyperdrive(...)` provisions a Hyperdrive config at deploy from a production connection string; your dev loop keeps using your normal Aspire database. Hosting-only (Worker-binding consumption — no .NET client).

Cross-cutting:

- **API-token first** — the Cloudflare API token is an Aspire secret parameter, and its permission scopes are validated up front (fail-fast) before anything is provisioned.
- **Custom domains** — attach a custom domain (with your Zone Id) to a Container or Pages app during deploy.

> Status: active development. R2, D1, AI, Vectorize, KV, and Queues (each hosting + client, with a local emulator via `.RunAsEmulator()`), Hyperdrive (hosting-only), the deploy-pipeline hijack, Cloudflare Pages, Cloudflare Containers, and custom domains are implemented. Live `aspire deploy` against a Cloudflare account is exercised via gated integration tests.

## Packages

| Package | Description |
| --- | --- |
| `AvantiPoint.Aspire.Hosting.Cloudflare` | Core hosting integration: the Cloudflare deploy environment, the publish/deploy pipeline hijack, token/scope validation, the Container target, and custom domains. |
| `AvantiPoint.Aspire.Hosting.Cloudflare.R2` | R2 bucket hosting: provisioning via the Cloudflare API plus a local MinIO S3 emulator (`.RunAsEmulator()`) for `aspire run`. |
| `AvantiPoint.Aspire.Hosting.Cloudflare.D1` | D1 database hosting: provisioning via the Cloudflare API plus a local SQLite emulator (`.RunAsEmulator()`) for `aspire run`. |
| `AvantiPoint.Aspire.Hosting.Cloudflare.Pages` | Cloudflare Pages hosting: `.PublishAsCloudflarePages(...)` attaches to an Aspire JavaScript app (`AddViteApp`/`AddNodeApp`) and deploys its build output. |
| `AvantiPoint.Aspire.Cloudflare.R2` | R2 **client** integration: registers a R2-tuned `IAmazonS3` (and `IR2Client`) from the Aspire-injected connection string. |
| `AvantiPoint.Aspire.Cloudflare.D1` | D1 **client** integration: registers an `ID1Client` (SQLite locally, the D1 HTTP API deployed) from the Aspire-injected connection string. |
| `AvantiPoint.Aspire.Hosting.Cloudflare.AI` | Cloudflare AI hosting: models a Workers AI / AI Gateway resource and runs a local Ollama server (`.RunAsEmulator()`) for `aspire run`. |
| `AvantiPoint.Aspire.Cloudflare.AI` | AI **client** integration: registers `Microsoft.Extensions.AI` `IChatClient`/`IEmbeddingGenerator` (Ollama locally, Cloudflare deployed) from the Aspire-injected connection string. |
| `AvantiPoint.Aspire.Hosting.Cloudflare.Vectorize` | Vectorize hosting: provisioning via the Cloudflare API plus an in-memory vector store (`.RunAsEmulator()`) for `aspire run`. |
| `AvantiPoint.Aspire.Cloudflare.Vectorize` | Vectorize **client** integration: registers an `IVectorizeClient` (in-memory locally, the Vectorize v2 HTTP API deployed) from the Aspire-injected connection string. |
| `AvantiPoint.Aspire.Hosting.Cloudflare.KV` | Workers KV hosting: provisioning via the Cloudflare API plus an in-memory store (`.RunAsEmulator()`) for `aspire run`. |
| `AvantiPoint.Aspire.Cloudflare.KV` | Workers KV **client** integration: registers an `ICloudflareKVClient` (in-memory locally, the KV HTTP API deployed). |
| `AvantiPoint.Aspire.Hosting.Cloudflare.Queues` | Queues hosting: provisioning via the Cloudflare API plus an in-memory queue (`.RunAsEmulator()`) for `aspire run`. |
| `AvantiPoint.Aspire.Cloudflare.Queues` | Queues **client** integration: registers an `IQueueClient` (in-memory locally, the Queues HTTP API deployed). |
| `AvantiPoint.Aspire.Hosting.Cloudflare.Hyperdrive` | Hyperdrive hosting (hosting-only): `PublishAsHyperdrive(...)` provisions a Hyperdrive config at deploy from a production database connection string. |
| `AvantiPoint.Aspire.Hosting.Extensions` | General Aspire host helpers, e.g. `AddDeploymentParameter` — parameters required at deploy but optional in local development. |

## Quickstart

```csharp
// AppHost
var builder = DistributedApplication.CreateBuilder(args);

builder.AddCloudflareEnvironment();           // resources below resolve it automatically

// Deploy-time config (Zone ID + hostnames) as parameters — absent in dev, required at deploy.
var zone    = builder.AddDeploymentParameter("zone-id");
var apiHost = builder.AddDeploymentParameter("api-hostname");
var webHost = builder.AddDeploymentParameter("web-hostname");

var uploads = builder.AddR2Bucket("uploads").RunAsEmulator();   // local MinIO during `aspire run`

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(uploads)
    .PublishAsCloudflareContainer()
    .WithCustomDomain(apiHost, zone);

// Pages attaches to a JavaScript app Aspire already models — not a raw folder.
builder.AddViteApp("web", "../Web")
    .PublishAsCloudflarePages()
    .WithCustomDomain(webHost, zone);

builder.Build().Run();
```

```csharp
// Consuming service
builder.AddR2Client("uploads");               // registers IR2Client (and IAmazonS3) for R2

// IR2Client is bound to the bucket — no bucket name to pass around:
app.MapGet("/data", (IR2Client r2) => r2.GetObjectAsync("data.json"));
```

Set `CLOUDFLARE_API_TOKEN` and `CLOUDFLARE_ACCOUNT_ID` (env vars or user-secrets) before `aspire deploy`.

## Development

```bash
dotnet build AvantiPoint.Aspire.slnx
dotnet test  AvantiPoint.Aspire.slnx
```

Integration tests are skipped unless `CLOUDFLARE_API_TOKEN` and `CLOUDFLARE_ACCOUNT_ID` are set.

Requirements: .NET 10 SDK, [wrangler](https://developers.cloudflare.com/workers/wrangler/) (for Pages/Container deploys), and Docker (for the MinIO emulator and container image builds).

## License

[MIT](LICENSE)
