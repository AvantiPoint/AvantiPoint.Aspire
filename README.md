# AvantiPoint Aspire for Cloudflare

[![CI](https://github.com/AvantiPoint/AvantiPoint.Aspire/actions/workflows/ci.yml/badge.svg)](https://github.com/AvantiPoint/AvantiPoint.Aspire/actions/workflows/ci.yml)
[![Docs](https://github.com/AvantiPoint/AvantiPoint.Aspire/actions/workflows/docs.yml/badge.svg)](https://github.com/AvantiPoint/AvantiPoint.Aspire/actions/workflows/docs.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

📖 **[Documentation & getting-started guides](https://avantipoint.github.io/AvantiPoint.Aspire/)** — finding your Account/Zone IDs, scoping API tokens, R2 credentials, wrangler auth, and more.

Hosting and .NET client integrations for modeling, provisioning and deploying Cloudflare resources with [Aspire](https://aspire.dev). Use local emulators during development and `aspire deploy` for Cloudflare deployment.

See the [service overview](https://avantipoint.github.io/AvantiPoint.Aspire/cloudflare/getting-started/introduction/) for supported resources and emulator behavior. The project is under active development. Preflight verifies token activeness; service APIs enforce permissions during provisioning and deployment.

## Packages

| Package | Description |
| --- | --- |
| `AvantiPoint.Aspire.Hosting.Cloudflare` | Core hosting integration: the Cloudflare deploy environment, the publish/deploy pipeline, token verification, the Container target, and custom domains. |
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

## Getting started

Follow the [quickstart](https://avantipoint.github.io/AvantiPoint.Aspire/cloudflare/getting-started/quickstart/) for package installation, AppHost imports, credentials and a complete R2/API/Pages example. Hosting packages belong in the AppHost; client packages belong in consuming .NET services.

## Development

See [CONTRIBUTING.md](CONTRIBUTING.md) for build and test commands, integration-test requirements and contribution guidance. See [SECURITY.md](SECURITY.md) before sharing deployment artifacts or reporting a vulnerability.

## TypeScript AppHosts

The hosting integrations also support official Aspire 13.6 TypeScript AppHosts through generated SDK exports. See the [TypeScript sample](samples/typescript/README.md) for local project references, callback configuration and credential-free interop validation. Existing C# AppHost APIs remain available.

## License

[MIT](LICENSE)
