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
- **R2 buckets** — provisioned via the Cloudflare API, with S3-compatible connection details flowed back to your apps. A local **MinIO** emulator runs during `aspire run` so the inner dev loop needs no cloud credentials.

Cross-cutting:

- **API-token first** — the Cloudflare API token is an Aspire secret parameter, and its permission scopes are validated up front (fail-fast) before anything is provisioned.
- **Custom domains** — attach a custom domain (with your Zone Id) to a Container or Pages app during deploy.

> Status: active development. R2 (hosting + client + MinIO emulator), the deploy-pipeline hijack, Cloudflare Pages, Cloudflare Containers, and custom domains are implemented. Live `aspire deploy` against a Cloudflare account is exercised via gated integration tests.

## Packages

| Package | Description |
| --- | --- |
| `AvantiPoint.Aspire.Hosting.Cloudflare` | Core hosting integration: the Cloudflare deploy environment, the publish/deploy pipeline hijack, token/scope validation, the Container target, and custom domains. |
| `AvantiPoint.Aspire.Hosting.Cloudflare.R2` | R2 bucket hosting: provisioning via the Cloudflare API plus a local MinIO S3 emulator for `aspire run`. |
| `AvantiPoint.Aspire.Hosting.Cloudflare.Pages` | Cloudflare Pages hosting: `.PublishAsCloudflarePages(...)` attaches to an Aspire JavaScript app (`AddViteApp`/`AddNodeApp`) and deploys its build output. |
| `AvantiPoint.Aspire.Cloudflare.R2` | R2 **client** integration: registers a R2-tuned `IAmazonS3` from the Aspire-injected connection string. |

## Quickstart

```csharp
// AppHost
var builder = DistributedApplication.CreateBuilder(args);

builder.AddCloudflareEnvironment();           // resources below resolve it automatically
var zone = builder.AddParameter("zone-id");   // Zone ID as config, not a literal

var uploads = builder.AddR2Bucket("uploads");

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(uploads)
    .PublishAsCloudflareContainer()
    .WithCustomDomain("api.example.com", zone);

// Pages attaches to a JavaScript app Aspire already models — not a raw folder.
builder.AddViteApp("web", "../Web")
    .PublishAsCloudflarePages()
    .WithCustomDomain("www.example.com", zone);

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
