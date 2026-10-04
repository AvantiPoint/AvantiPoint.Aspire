---
title: "API Tokens & Scopes"
---


The integration (and `wrangler`) authenticate to Cloudflare with an **API token**. This is the single most important piece to get right: the token must carry the permissions for the services you deploy — no more, no less.

:::note
This API token is **not** the same as the R2 S3 credentials your app uses at runtime. Those are separate — see [R2 S3 Credentials](../r2-credentials/).
:::


## Create a token

1. The current integration verifies tokens through `/user/tokens/verify`. Create a user API token at **[My Profile → API Tokens](https://dash.cloudflare.com/profile/api-tokens)**. Account-owned token verification uses a different endpoint and is not currently supported by this preflight.
2. Select **Create Token → Create Custom Token**.
3. Add the **permissions** for the services you'll deploy (see the table below).
4. Set **Account Resources** to your account, and **Zone Resources** to the specific zone(s) only if you use custom domains.
5. Create the token and **copy it now** — Cloudflare shows the secret only once.

Provide it to the integration via the `CLOUDFLARE_API_TOKEN` environment variable (or an Aspire parameter — see [Quickstart](../quickstart/)).

## Which permissions do I need?

The AppHost aggregates the permissions its resources need. Preflight verification checks whether the token is active; it does not inspect its permission policy. Cloudflare service APIs enforce permissions during provisioning and deployment. Grant the union of the rows that apply:

| If you deploy… | Permission group | Level | Category |
| --- | --- | --- | --- |
| **Anything** (always) | User Details | Read | User |
| **R2 buckets** (`…Hosting.Cloudflare.R2`) | Workers R2 Storage | Edit | Account |
| **Containers / .NET APIs** (`…Hosting.Cloudflare`) | Workers Scripts | Edit | Account |
| **D1 databases** | D1 | Edit | Account |
| **Workers AI** | Workers AI | Read | Account |
| **AI Gateway** | AI Gateway | Edit | Account |
| **Vectorize indexes** | Vectorize | Edit | Account |
| **Workers KV namespaces** | Workers KV Storage | Edit | Account |
| **Queues** | Queues | Edit | Account |
| **Hyperdrive configurations** | Hyperdrive | Edit | Account |
| **Pages** (`…Hosting.Cloudflare.Pages`) | Cloudflare Pages | Edit | Account |
| **Custom domains** (`WithCustomDomain`) | DNS | Edit | Zone |
| **Custom domains** (`WithCustomDomain`) | Zone | Read | Zone |

Hand-authored Workers also need **Workers Scripts: Edit** to deploy. Wrangler's Worker deletion command may additionally list legacy Workers Sites KV namespaces and require **Workers KV Storage: Read**, even for a Worker without KV bindings. This is a teardown requirement, not an R2 or Worker deployment requirement. If that auxiliary check fails after the Worker has been deleted, verify the Worker is absent before retrying or changing token permissions.

:::note
Cloudflare groups permissions into **Account**, **Zone**, and **User** categories, each granted at **Edit** or **Read**. Permission-group *names* are cosmetic and can change in the dashboard; if a name differs, search for the closest match in the right category. The authoritative, current list is the [API token permissions reference](https://developers.cloudflare.com/fundamentals/api/reference/permissions/).
:::


## Minimal tokens by scenario

- **R2 only:** *User Details: Read* + *Workers R2 Storage: Edit*.
- **API container + R2:** add *Workers Scripts: Edit*.
- **Full stack (API + Pages + R2):** add *Cloudflare Pages: Edit*.
- **+ custom domains:** add *DNS: Edit* and *Zone: Read*, scoped to the relevant zone(s).

## How validation works

On `aspire deploy`, the first pipeline step calls Cloudflare's [token verify](https://developers.cloudflare.com/fundamentals/api/how-to/test-token/) endpoint. If the token is missing, inactive, or rejected, the deploy stops immediately with a message listing the scopes the operation expected — so you can fix the token before anything is provisioned.

A successful preflight does **not** establish that the token has these scopes or access to the intended account and zones. An active but under-scoped token can fail later, after earlier resources have been provisioned.

## Security tips

- Use a dedicated, narrowly scoped token for CI, and review access when its owner changes.
- Scope **Zone Resources** to specific zones rather than *All zones* when you can.
- Store the token as a **secret** (GitHub Actions secret, user-secrets) — never commit it.
- Rotate tokens periodically; you can roll or delete them from the same dashboard page.
