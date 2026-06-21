---
title: "Introduction"
---


AvantiPoint Aspire for Cloudflare lets you model your Cloudflare resources in your Aspire AppHost and deploy to Cloudflare with `aspire deploy` — the same way the AWS integration targets AWS instead of Azure.

## How it works

When you add a **Cloudflare environment** to your AppHost, the integration hooks into Aspire's publish/deploy pipeline:

1. **Validate** — your Cloudflare API token is checked up front; deployment fails fast with a clear message if it's missing, invalid, or under-scoped.
2. **Publish** — build artifacts are produced (container images, generated `wrangler.jsonc`, the JS build output).
3. **Deploy** — resources are created/updated on Cloudflare: R2 buckets via the REST API, Workers/Containers and Pages via `wrangler`.

During local `aspire run`, R2 is backed by a local **MinIO** emulator, so the inner dev loop needs no Cloudflare credentials at all.

## What you'll set up

Most of the friction in getting started is on the Cloudflare side — gathering identifiers and minting a token with the right permissions. The next pages walk through each piece:

| Step | Why |
| --- | --- |
| [Prerequisites](prerequisites.md) | Tools you need installed. |
| [Account ID & Zone ID](account-and-zone-ids.md) | Required for provisioning and custom domains. |
| [API Tokens & Scopes](api-tokens.md) | The token the integration and `wrangler` use — scoped to the services you deploy. |
| [R2 S3 Credentials](r2-credentials.md) | Separate keys your app uses to read/write R2 at runtime. |
| [How wrangler authenticates](wrangler.md) | Non-interactive auth for Pages/Containers. |
| [Quickstart](quickstart.md) | Wire it all together. |
