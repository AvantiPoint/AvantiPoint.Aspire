---
_layout: landing
---

# AvantiPoint Aspire for Cloudflare

Provision and deploy your [.NET Aspire](https://aspire.dev) applications to [Cloudflare](https://www.cloudflare.com/) — instead of Azure. `aspire deploy` is hijacked so your distributed app ships to Cloudflare:

- **.NET APIs → Cloudflare Containers**
- **JavaScript frontends → Cloudflare Pages**
- **R2 buckets** — provisioned via the Cloudflare API, with a local **MinIO** emulator for the dev loop

## New here?

Start with **[Getting Started](getting-started/introduction.md)**. The most important setup steps are gathering your Cloudflare identifiers and minting a correctly-scoped API token:

- [Find your Account ID and Zone ID](getting-started/account-and-zone-ids.md)
- [Create and scope an API token](getting-started/api-tokens.md)
- [R2 S3 credentials](getting-started/r2-credentials.md)
- [How wrangler authenticates](getting-started/wrangler.md)
- [Quickstart](getting-started/quickstart.md)

## Packages

| Package | Purpose |
| --- | --- |
| `AvantiPoint.Aspire.Hosting.Cloudflare` | Core: the Cloudflare deploy environment, pipeline hijack, token validation, Containers, custom domains. |
| `AvantiPoint.Aspire.Hosting.Cloudflare.R2` | R2 bucket hosting + local MinIO emulator. |
| `AvantiPoint.Aspire.Hosting.Cloudflare.Pages` | Cloudflare Pages — attaches to an Aspire JavaScript app. |
| `AvantiPoint.Aspire.Cloudflare.R2` | R2 client — registers a R2-tuned `IAmazonS3`. |
