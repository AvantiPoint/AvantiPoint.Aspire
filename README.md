# AvantiPoint Aspire for Cloudflare

[![CI](https://github.com/AvantiPoint/AvantiPoint.Aspire/actions/workflows/ci.yml/badge.svg)](https://github.com/AvantiPoint/AvantiPoint.Aspire/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

.NET Aspire hosting and client integrations that make Aspire a first-class way to **provision and deploy to [Cloudflare](https://www.cloudflare.com/)** — the same way the AWS integration targets AWS, instead of Azure.

`aspire deploy` is hijacked so your distributed application is published and deployed to Cloudflare:

- **.NET APIs → Cloudflare Containers** — a container image is built from your `ProjectResource` and run on Cloudflare Containers.
- **JavaScript frontends → Cloudflare Pages** — your build output directory is deployed to Pages.
- **R2 buckets** — provisioned via the Cloudflare API, with S3-compatible connection details flowed back to your apps. A local **MinIO** emulator runs during `aspire run` so the inner dev loop needs no cloud credentials.

Cross-cutting:

- **API-token first** — the Cloudflare API token is an Aspire secret parameter, and its permission scopes are validated up front (fail-fast) before anything is provisioned.
- **Custom domains** — attach a custom domain (with your Zone Id) to a Container or Pages app during deploy.

> Status: early development. See [the implementation plan](#packages) and milestones below.

## Packages

| Package | Description |
| --- | --- |
| `AvantiPoint.Aspire.Hosting.Cloudflare` | Core hosting integration: the Cloudflare deploy environment, the publish/deploy pipeline hijack, token/scope validation, Container & Pages resources, and custom domains. |
| `AvantiPoint.Aspire.Hosting.Cloudflare.R2` | R2 bucket hosting: provisioning via the Cloudflare API plus a local MinIO S3 emulator for `aspire run`. |
| `AvantiPoint.Aspire.Cloudflare.R2` | R2 **client** integration: registers a R2-tuned `IAmazonS3` from the Aspire-injected connection string. |

## Quickstart

```csharp
// AppHost
var builder = DistributedApplication.CreateBuilder(args);

var cloudflare = builder.AddCloudflareEnvironment("cloudflare");

var uploads = cloudflare.AddR2Bucket("uploads");

var api = builder.AddProject<Projects.Api>("api")
    .PublishAsCloudflareContainer(cloudflare)
    .WithReference(uploads)
    .WithCustomDomain(zoneId: "<zone-id>", hostname: "api.example.com");

builder.AddCloudflarePages("web", cloudflare, buildOutputPath: "../Web/dist")
    .WithCustomDomain(zoneId: "<zone-id>", hostname: "www.example.com");

builder.Build().Run();
```

```csharp
// Consuming service
builder.AddR2Client("uploads"); // registers IAmazonS3 configured for R2 (works against MinIO locally)
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
